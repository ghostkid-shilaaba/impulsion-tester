import sys
import json
import time
import math
import pyvisa
from pathlib import Path
import re

CONFIG_FOLDER = Path(__file__).parent / "instrument_configs"

ATTENUATION_STEPS = [
    {"db": 2.0, "iso_target": 80.0},
    {"db": 1.0, "iso_target": 90.0},
    {"db": 0.0, "iso_target": 100.0},
    {"db": 4.0, "iso_target": 64.0},
    {"db": 6.0, "iso_target": 50.0},
    {"db": 8.0,  "iso_target": 40.0},
    {"db": 12.0, "iso_target": 25.0},
    {"db": 14.0, "iso_target": 20.0},
    {"db": 20.0, "iso_target": 10.0},
    {"db": 26.0, "iso_target": 5.0},
]

REFERENCE_PERCENT = 80.0

MSG_SETUP_REFERENCE = "setup_reference"
MSG_CHANGE_ATTENUATOR = "change_attenuator"
MSG_ENTER_SCREEN_READ = "enter_screen_read"

def debug(msg):
    print(f"DEBUG: {msg}", file=sys.stderr, flush=True)

def extract_prefix(model):
    m = re.match(r'^([A-Za-z]+\d)', model)
    return m.group(1).upper() if m else model.upper()

class ConfigManager:
    def __init__(self, config_folder):
        self.config_folder = config_folder
        self.cache = {}
        self._load_all()

    def _load_all(self):
        if not self.config_folder.exists():
            return
        for json_file in self.config_folder.glob("*.json"):
            try:
                with open(json_file, "r", encoding="utf-8") as f:
                    prefix = json_file.stem.upper()
                    self.cache[prefix] = json.load(f)
                    debug(f"Loaded config: {json_file.name} -> prefix '{prefix}'")
            except Exception as e:
                debug(f"Failed to load {json_file.name}: {e}")

    def get(self, prefix):
        return self.cache.get(prefix)

def get_command(cfg, name, **values):
    if cfg is None:
        return None
    cmds = cfg.get("commands", {})
    if name not in cmds:
        debug(f"WARNING: Command '{name}' not found in config")
        return None
    cmd_obj = cmds[name]
    cmd = cmd_obj.get("command", "") if isinstance(cmd_obj, dict) else cmd_obj

    for k, v in values.items():
        token = "{" + k + "}"
        if token in cmd:
            cmd = cmd.replace(token, str(v))

    return cmd

def safe_write(inst, cmd):
    if not cmd:
        return False
    try:
        inst.write(cmd)
        try:
            inst.query("*OPC?")
        except Exception:
            time.sleep(0.05)
        return True
    except Exception as e:
        debug(f"Write failed for '{cmd}': {e}")
        return False

def safe_query_float(inst, cmd, retries=3):
    if not cmd:
        return None
    for attempt in range(retries):
        try:
            raw = inst.query(cmd).strip()
            value = float(raw)
            if math.isnan(value):
                debug(f"Got NaN from '{cmd}'")
                if attempt < retries - 1:
                    time.sleep(0.2)
                    continue
                return None
            if abs(value) > 1e30:
                debug(f"Got overflow value {value} from '{cmd}'")
                if attempt < retries - 1:
                    time.sleep(0.2)
                    continue
                return None
            return value
        except ValueError:
            debug(f"Could not convert '{raw}' to float from '{cmd}'")
            if attempt < retries - 1:
                time.sleep(0.2)
            else:
                debug(f"Failed to query '{cmd}' after {retries} attempts")
                return None
        except Exception:
            if attempt < retries - 1:
                time.sleep(0.2)
            else:
                debug(f"Failed to query '{cmd}' after {retries} attempts")
                return None
    return None

def check_error_queue(inst):
    errors = []
    try:
        while True:
            err = inst.query(":SYST:ERR?").strip()
            if err.startswith("0"):
                break
            errors.append(err)
            debug(f"Instrument error: {err}")
        if errors:
            debug(f"Total errors cleared: {len(errors)}")
        return errors
    except Exception as e:
        debug(f"Failed to check errors: {e}")
        return None

def apply_scope_base_config(inst, cfg):
    meta = cfg.get("meta", {})
    inst_type = meta.get("deviceType", "unknown")
    if inst_type != "oscilloscope":
        debug(f"apply_scope_base_config called for non-oscilloscope ({inst_type}) -- skipping")
        return False

    debug("Applying oscilloscope base config")
    try:
        channel_display_cmd = get_command(cfg, "channel_display", channel=1)
        if channel_display_cmd:
            safe_write(inst, f"{channel_display_cmd} ON")
        else:
            debug("WARNING: 'channel_display' command not found")

        channel_scale_cmd = get_command(cfg, "channel_scale", channel=1)
        if channel_scale_cmd:
            safe_write(inst, f"{channel_scale_cmd} 1")
        else:
            debug("WARNING: 'channel_scale' command not found")

        channel_impedance_cmd = get_command(cfg, "channel_impedance", channel=1)
        if channel_impedance_cmd:
            safe_write(inst, f"{channel_impedance_cmd} OMEG")
        else:
            debug("WARNING: 'channel_impedance' command not found")

        # 200 ns/div horizontal -- was never set here, so the scope kept
        # whatever timebase the previous test (LDG, RFA...) left it in.
        timebase_scale_cmd = get_command(cfg, "timebase_scale")
        if timebase_scale_cmd:
            safe_write(inst, f"{timebase_scale_cmd} 2e-7")
        else:
            debug("WARNING: 'timebase_scale' command not found")

        channel_offset_cmd = get_command(cfg, "channel_offset", channel=1)
        if channel_offset_cmd:
            safe_write(inst, f"{channel_offset_cmd} 0")
        else:
            debug("WARNING: 'channel_offset' command not found")

        trigger_mode_cmd = get_command(cfg, "trigger_mode")
        if trigger_mode_cmd:
            safe_write(inst, f"{trigger_mode_cmd} EDGe")
        else:
            debug("WARNING: 'trigger_mode' command not found")

        trigger_source_cmd = get_command(cfg, "trigger_source")
        if trigger_source_cmd:
            safe_write(inst, f"{trigger_source_cmd} EXT")
        else:
            debug("WARNING: 'trigger_source' command not found")

        trigger_slope_cmd = get_command(cfg, "trigger_slope")
        if trigger_slope_cmd:
            safe_write(inst, f"{trigger_slope_cmd} POSitive")
        else:
            debug("WARNING: 'trigger_slope' command not found")

         #--- FIX: Clear only ITEM2 to ITEM5 (keeping ITEM1) ---
        measurement_clear_cmd = get_command(cfg, "measurement_clear")
        if measurement_clear_cmd:
    # Loop through ITEM2 to ITEM5
            for i in range(2, 6):
                safe_write(inst, f"{measurement_clear_cmd} ITEM{i}")
                debug("Cleared ITEM2-ITEM5, keeping VPP measurement.")
        else:
            debug("WARNING: 'measurement_clear' command not found")

        time.sleep(0.5)
        check_error_queue(inst)
        debug("Oscilloscope configured: 1V/div, 1MOhm, EXTernal trigger, POSitive slope, centered, VPP-only display")
        return True
    except Exception as e:
        debug(f"Failed to apply scope base config: {e}")
        return False

def set_measurement_source_once(scope, scope_cfg, channel=1):
    src_cmd = get_command(scope_cfg, "measurement_source", source=f"CHANnel{channel}")
    if src_cmd: safe_write(scope, src_cmd)

    # apply_scope_base_config() just ran measurement_clear(ALL), which wipes
    # the on-screen measurement readout. MEASure:VPP? on its own only
    # returns a value to the script -- it does NOT put the item back on the
    # scope's display. There's no "measurement_item" entry in the
    # instrument_configs JSON files, so this writes the raw Rigol SCPI
    # command directly to re-add VPP to the on-screen list for this channel.
    safe_write(scope, f"MEASure:ITEM VPP,CHANnel{channel}")

    return True

def measure_vpp_single(scope, scope_cfg):
    """Return a single Vpp measurement, no averaging."""
    vpp_cmd = get_command(scope_cfg, "measurement_vpp")
    if not vpp_cmd:
        return None
    return safe_query_float(scope, vpp_cmd, retries=2)

def setup_generator(gen, gen_cfg, channel, frequency, voltage):
    func_cmd = get_command(gen_cfg, "function_shape", channel=channel)
    if func_cmd: safe_write(gen, f"{func_cmd} SINusoid")

    freq_cmd = get_command(gen_cfg, "frequency_fixed", channel=channel)
    if freq_cmd: safe_write(gen, f"{freq_cmd} {frequency}")
    amp_cmd = get_command(gen_cfg, "voltage_amplitude", channel=channel)
    if amp_cmd: safe_write(gen, f"{amp_cmd} {voltage}")

    burst_cmd = get_command(gen_cfg, "burst_state", channel=channel)
    if burst_cmd:
        safe_write(gen, f"{burst_cmd} ON")
    else:
        debug("WARNING: 'burst_state' command not found")

    burst_mode_cmd = get_command(gen_cfg, "burst_mode", channel=channel)
    if burst_mode_cmd:
        safe_write(gen, f"{burst_mode_cmd} TRIGgered")
    else:
        debug("WARNING: 'burst_mode' command not found")

    burst_ncycles_cmd = get_command(gen_cfg, "burst_ncycles", channel=channel, ncycles=11)
    if burst_ncycles_cmd:
        safe_write(gen, burst_ncycles_cmd)
    else:
        debug("WARNING: 'burst_ncycles' command not found")

    burst_trigger_cmd = get_command(gen_cfg, "burst_trigger_source", channel=channel, source="EXTernal")
    if burst_trigger_cmd:
        safe_write(gen, burst_trigger_cmd)
    else:
        debug("WARNING: 'burst_trigger_source' command not found")

    burst_trigger_slope_cmd = get_command(gen_cfg, "output_trigger_slope", channel=channel)
    if burst_trigger_slope_cmd:
        safe_write(gen, f"{burst_trigger_slope_cmd} POSitive")
    else:
        debug("WARNING: 'output_trigger_slope' command not found in config")

    out_cmd = get_command(gen_cfg, "output_state", channel=channel)
    if out_cmd:
        safe_write(gen, f"{out_cmd} ON")
        check_error_queue(gen)
    time.sleep(0.3)

def ask_operator_for_screen_read(measured_voltage, calculated_percent, iso_target):
    print(json.dumps({
        "type": MSG_ENTER_SCREEN_READ,
        "measured_voltage": measured_voltage,
        "calculated_percent": calculated_percent,
        "iso_target": iso_target,
        "message": f"Tension mesuree: {measured_voltage:.3f} V\n" +
                   f"ISO Target: {iso_target:.1f}%\n" +
                   f"% ecran calcule: {calculated_percent:.3f}%\n\n" +
                   "Veuillez entrer le % ecran lu sur l'ecran:"
    }), flush=True)
    while True:
        try:
            line = sys.stdin.readline()
            if not line: raise Exception("VB.NET disconnected (stdin closed)")
            data = json.loads(line.strip())
            if data.get("action") == "screen_read":
                screen_read = data.get("value")
                if screen_read is not None: return float(screen_read)
            elif data.get("action") == "cancel":
                raise Exception("Measurement cancelled by user")
        except json.JSONDecodeError:
            time.sleep(0.1)

def run_lva_measurement():
    try:
        line = sys.stdin.readline()
        if not line:
            print(json.dumps({"success": False, "error": "Missing payload on stdin."}), flush=True)
            return
        payload = json.loads(line.strip())
    except Exception as e:
        print(json.dumps({"success": False, "error": f"Failed to parse payload: {str(e)}"}), flush=True)
        return

    try:
        config_manager = ConfigManager(CONFIG_FOLDER)
        rm = pyvisa.ResourceManager()
    except Exception as e:
        print(json.dumps({"success": False, "error": f"Impossible d'initialiser VISA : {str(e)}"}), flush=True)
        return
    scope = None
    gen = None
    gen_cfg = None
    channel = 1

    try:
        frequency = payload.get("frequency") or payload.get("frequency_mhz")
        if frequency is None: raise Exception("Missing 'frequency' in payload")
        initial_voltage = payload.get("voltage") or payload.get("voltage_vcc")
        if initial_voltage is None: raise Exception("Missing 'voltage' in payload")

        gain = payload.get("gain")
        channel = payload.get("channel", 1)
        reference_percent = payload.get("reference_percent", 80.0)

        scope_resource = payload.get("scope_resource")
        if not scope_resource: raise Exception("Missing scope resource")
        scope = rm.open_resource(scope_resource)
        scope.timeout = 10000

        scope_cfg = payload.get("scope_config")
        if scope_cfg is None:
            debug("WARNING: No scope_config...")
            scope_idn = scope.query("*IDN?").strip()
            scope_parts = scope_idn.split(",")
            if len(scope_parts) >= 2:
                scope_model = scope_parts[1].strip()
                scope_prefix = extract_prefix(scope_model)
                scope_cfg = config_manager.get(scope_prefix)
            else:
                raise Exception(f"Could not parse IDN: {scope_idn}")
        if scope_cfg is None: raise Exception("No configuration found for oscilloscope")

        apply_scope_base_config(scope, scope_cfg)
        set_measurement_source_once(scope, scope_cfg, channel)

        gen_resource = payload.get("generator_resource")
        if not gen_resource: raise Exception("Missing generator resource")
        gen = rm.open_resource(gen_resource)
        gen.timeout = 10000

        gen_cfg = payload.get("generator_config")
        if gen_cfg is None:
            debug("WARNING: No generator_config...")
            gen_idn = gen.query("*IDN?").strip()
            gen_parts = gen_idn.split(",")
            if len(gen_parts) >= 2:
                gen_model = gen_parts[1].strip()
                gen_prefix = extract_prefix(gen_model)
                gen_cfg = config_manager.get(gen_prefix)
            else:
                raise Exception(f"Could not parse IDN: {gen_idn}")
        if gen_cfg is None: raise Exception("No configuration found for generator")

        setup_generator(gen, gen_cfg, channel, frequency * 1e6, initial_voltage)
        time.sleep(0.5)

        gain_msg = f" ({gain} dB)" if gain else ""
        print(json.dumps({
            "type": MSG_SETUP_REFERENCE,
            "target_percent": reference_percent,
            "initial_voltage": initial_voltage,
            "frequency": frequency,
            "gain": gain,
            "message": f"1. Reglez l'attenuateur sur +2 dB.\n" +
                       f"2. Reglez le gain de l'appareil U.T sur {gain_msg}.\n" +
                       f"3. Reglez la tension du generateur pour obtenir un signal a {reference_percent}% de la hauteur de l'ecran. \n" +
                       "4. Cliquez sur OK une fois le reglage effectue."
        }), flush=True)

        confirmed = False
        while not confirmed:
            line = sys.stdin.readline()
            if not line: raise Exception("VB.NET disconnected (stdin closed)")
            try:
                data = json.loads(line.strip())
                if data.get("action") == "confirm": confirmed = True
                elif data.get("action") == "cancel": raise Exception("Measurement cancelled by user")
            except json.JSONDecodeError:
                time.sleep(0.1)

        # ---- REFERENCE VOLTAGE: SINGLE READING (no averaging) ----
        reference_voltage = measure_vpp_single(scope, scope_cfg)
        if reference_voltage is None:
            raise Exception("Failed to measure reference voltage")

        debug(f"Reference voltage ({reference_percent}%): {reference_voltage:.3f}V")

        results = []
        for idx, step in enumerate(ATTENUATION_STEPS):
            db = step["db"]
            iso_target = step["iso_target"]

            print(json.dumps({
                "type": MSG_CHANGE_ATTENUATOR,
                "db": db,
                "iso_target": iso_target,
                "progress": idx + 1,
                "total": len(ATTENUATION_STEPS),
                "message": f"Reglez l'attenuateur sur {db:+.1f} dB, puis cliquez sur OK"
            }), flush=True)

            confirmed = False
            while not confirmed:
                line = sys.stdin.readline()
                if not line: raise Exception("VB.NET disconnected (stdin closed)")
                try:
                    data = json.loads(line.strip())
                    if data.get("action") == "confirm": confirmed = True
                    elif data.get("action") == "cancel": raise Exception("Measurement cancelled by user")
                except json.JSONDecodeError:
                    time.sleep(0.1)

            # ---- MEASURED VOLTAGE: SINGLE READING (no averaging) ----
            measured_voltage = measure_vpp_single(scope, scope_cfg)
            if measured_voltage is None:
                raise Exception(f"Unable to measure Vpp at {db} dB")

            calculated_percent = (measured_voltage / reference_voltage) * reference_percent
            screen_read = ask_operator_for_screen_read(measured_voltage, calculated_percent, iso_target)
            if screen_read is None: raise Exception(f"Failed to get screen read at {db} dB")

            screen_error = calculated_percent - screen_read
            results.append({
                "db": db,
                "measured_voltage": round(measured_voltage, 3),
                "iso_target": iso_target,
                "calculated_percent": round(calculated_percent, 1),
                "screen_read": round(screen_read, 3),
                "screen_error": round(screen_error, 3),
                "in_tolerance": abs(screen_error) <= 2.0
            })
            debug(f"{db:+3.1f}dB: Error={screen_error:+.3f}%")

        if results:
            errors = [r["screen_error"] for r in results]
            max_error = max(errors, key=abs)
            max_error = round(max_error, 3)
        else:
            max_error = 0.0

        all_in_tolerance = all([r["in_tolerance"] for r in results]) if results else False

        out_cmd = get_command(gen_cfg, "output_state", channel=channel)
        if out_cmd: safe_write(gen, f"{out_cmd} OFF")

        output = {
            "success": True,
            "frequency": frequency,
            "initial_voltage": initial_voltage,
            "reference_voltage": reference_voltage,
            "reference_percent": reference_percent,
            "gain": gain,
            "max_error": max_error,
            "all_in_tolerance": all_in_tolerance,
            "results": results
        }
        print(json.dumps(output), flush=True)

    except Exception as e:
        print(json.dumps({"success": False, "error": str(e)}), flush=True)
    finally:
        try:
            if gen and gen_cfg:
                out_cmd = get_command(gen_cfg, "output_state", channel=channel)
                if out_cmd: safe_write(gen, f"{out_cmd} OFF")
        except Exception: pass
        try: scope.close()
        except Exception: pass
        try: gen.close()
        except Exception: pass
        try: rm.close()
        except Exception: pass

if __name__ == "__main__":
    run_lva_measurement()