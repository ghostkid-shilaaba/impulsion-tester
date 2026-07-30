import sys
import json
import time
import math
import pyvisa
from pathlib import Path
import re

CONFIG_FOLDER = Path(__file__).parent / "instrument_configs"

# Attenuation steps and their ISO target % screen heights
ATTENUATION_STEPS = [
    {"db": 2.0, "iso_target": 100.0},
    {"db": 1.0, "iso_target": 89.8},
    {"db": 0.0, "iso_target": 80.0},
    {"db": -2.0, "iso_target": 63.6},
    {"db": -4.0, "iso_target": 50.4},
    {"db": -6.0, "iso_target": 40.0},
    {"db": -10.0, "iso_target": 25.3},
    {"db": -12.0, "iso_target": 20.1},
    {"db": -18.0, "iso_target": 10.1},
    {"db": -24.0, "iso_target": 5.0},
]

REFERENCE_PERCENT = 80.0

# Message type constants to prevent typos
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

def apply_base_config(inst, cfg):
    base = cfg.get("baseconfig", {})
    if not base:
        debug("No baseconfig found - skipping")
        return False
    meta = cfg.get("meta", {})
    inst_type = meta.get("deviceType", "unknown")
    debug(f"Applying base config for {inst_type}")
    try:
        if inst_type == "oscilloscope":
            # ... [oscilloscope configuration] ...
            channel_display_cmd = get_command(cfg, "channel_display", channel=1)
            if channel_display_cmd: safe_write(inst, f"{channel_display_cmd} ON")
            channel_scale_cmd = get_command(cfg, "channel_scale", channel=1)
            scale_val = base.get("channel_scale", 0.5)
            if channel_scale_cmd: safe_write(inst, f"{channel_scale_cmd} {scale_val}")
            channel_impedance_cmd = get_command(cfg, "channel_impedance", channel=1)
            imp_val = base.get("channel_impedance", 50)
            if channel_impedance_cmd: safe_write(inst, f"{channel_impedance_cmd} {imp_val}")
            trigger_source_cmd = get_command(cfg, "trigger_source")
            src_val = base.get("trigger_source", "CH1")
            if trigger_source_cmd: safe_write(inst, f"{trigger_source_cmd} {src_val}")
            trigger_slope_cmd = get_command(cfg, "trigger_slope")
            slope_val = base.get("trigger_slope", "NEGative")
            if trigger_slope_cmd: safe_write(inst, f"{trigger_slope_cmd} {slope_val}")
            trigger_level_cmd = get_command(cfg, "trigger_level")
            level_val = base.get("trigger_level", 0.5)
            if trigger_level_cmd: safe_write(inst, f"{trigger_level_cmd} {level_val}")
            timebase_cmd = get_command(cfg, "timebase_scale")
            tb_val = base.get("timebase", 2e-8)
            if timebase_cmd: safe_write(inst, f"{timebase_cmd} {tb_val}")
            time.sleep(0.5)
            check_error_queue(inst)
            debug("Oscilloscope base config applied")
            return True
        elif inst_type == "generator":
            burst_cmd = get_command(cfg, "burst_state", channel=1)
            if burst_cmd: safe_write(inst, f"{burst_cmd} ON")
            burst_mode_cmd = get_command(cfg, "burst_mode", channel=1)
            if burst_mode_cmd: safe_write(inst, f"{burst_mode_cmd} TRIGgered")
            burst_ncycles_cmd = get_command(cfg, "burst_ncycles", channel=1)
            if burst_ncycles_cmd: safe_write(inst, f"{burst_ncycles_cmd} 11")
            burst_trigger_cmd = get_command(cfg, "burst_trigger_source", channel=1)
            if burst_trigger_cmd: safe_write(inst, f"{burst_trigger_cmd} EXTernal")
            func_cmd = get_command(cfg, "function_shape", channel=1)
            if func_cmd: safe_write(inst, f"{func_cmd} SINusoid")
            imp_cmd = get_command(cfg, "output_impedance", channel=1)
            imp_val = base.get("output_impedance", 50)
            if imp_cmd: safe_write(inst, f"{imp_cmd} {imp_val}")
            load_cmd = get_command(cfg, "output_load", channel=1)
            load_val = base.get("output_load", 50)
            if load_cmd: safe_write(inst, f"{load_cmd} {load_val}")
            out_cmd = get_command(cfg, "output_state", channel=1)
            if out_cmd: 
                safe_write(inst, f"{out_cmd} OFF") # Starts OFF
            check_error_queue(inst)
            debug("Generator base config applied (BURST ON, 11 cycles)")
            return True
        return False
    except Exception as e:
        debug(f"Failed to apply base config: {e}")
        return False

def set_measurement_source_once(scope, scope_cfg, channel=1):
    src_cmd = get_command(scope_cfg, "measurement_source", source=f"CHANnel{channel}")
    if src_cmd: safe_write(scope, src_cmd)
    return True

def measure_vpp_averaged(scope, scope_cfg, num_readings=3):
    vpp_cmd = get_command(scope_cfg, "measurement_vpp")
    if not vpp_cmd: return None
    values = []
    for _ in range(num_readings):
        v = safe_query_float(scope, vpp_cmd, retries=2)
        if v is not None: values.append(v)
        time.sleep(0.05)
    if not values: return None
    return sum(values) / len(values)

def setup_generator(gen, gen_cfg, channel, frequency, voltage):
    func_cmd = get_command(gen_cfg, "function_shape", channel=channel)
    if func_cmd: safe_write(gen, f"{func_cmd} SINusoid")
    freq_cmd = get_command(gen_cfg, "frequency_fixed", channel=channel)
    if freq_cmd: safe_write(gen, f"{freq_cmd} {frequency}")
    amp_cmd = get_command(gen_cfg, "voltage_amplitude", channel=channel)
    if amp_cmd: safe_write(gen, f"{amp_cmd} {voltage}")
    out_cmd = get_command(gen_cfg, "output_state", channel=channel)
    if out_cmd: 
        safe_write(gen, f"{out_cmd} ON")
        check_error_queue(gen) # FIX: Check if generator actually turned ON
    time.sleep(0.3)

def ask_operator_for_screen_read(measured_voltage, calculated_percent, iso_target):
    print(json.dumps({
        "type": MSG_ENTER_SCREEN_READ,
        "measured_voltage": measured_voltage,
        "calculated_percent": calculated_percent,
        "iso_target": iso_target,
        "message": f"Tension mesurée: {measured_voltage:.4f} V\n" +
                   f"ISO Target: {iso_target:.1f}%\n" +
                   f"% écran calculé: {calculated_percent:.1f}%\n\n" +
                   "Veuillez entrer le % écran lu sur l'oscilloscope:"
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

    config_manager = ConfigManager(CONFIG_FOLDER)
    rm = pyvisa.ResourceManager()
    scope = None
    gen = None

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

        apply_base_config(scope, scope_cfg)
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

        apply_base_config(gen, gen_cfg)

        setup_generator(gen, gen_cfg, channel, frequency, initial_voltage)
        time.sleep(0.5)

        # Step 2: Setup reference
        gain_msg = f" (gain recommandé: {gain} dB)" if gain else ""
        print(json.dumps({
            "type": MSG_SETUP_REFERENCE,
            "target_percent": reference_percent,
            "initial_voltage": initial_voltage,
            "frequency": frequency,
            "gain": gain,
            "message": f"1. Réglez l'atténuateur sur +2 dB.\n" +
                       f"2. Réglez le gain du DUT afin d'obtenir {reference_percent}% de hauteur d'écran{gain_msg}.\n" +
                       "3. Cliquez sur OK une fois le réglage effectué."
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

        reference_voltage = measure_vpp_averaged(scope, scope_cfg, num_readings=5)
        if reference_voltage is None: raise Exception("Failed to measure reference voltage")

        debug(f"Reference voltage ({reference_percent}%): {reference_voltage:.4f}V")

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
                "message": f"Réglez l'atténuateur sur {db:+.1f} dB, puis cliquez sur OK"
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

            measured_voltage = measure_vpp_averaged(scope, scope_cfg, num_readings=3)
            if measured_voltage is None: raise Exception(f"Unable to measure Vpp at {db} dB")

            calculated_percent = (measured_voltage / reference_voltage) * reference_percent
            screen_read = ask_operator_for_screen_read(measured_voltage, calculated_percent, iso_target)
            if screen_read is None: raise Exception(f"Failed to get screen read at {db} dB")

            screen_error = screen_read - calculated_percent
            results.append({
                "db": db, "measured_voltage": measured_voltage, "iso_target": iso_target,
                "calculated_percent": calculated_percent, "screen_read": screen_read,
                "screen_error": screen_error, "in_tolerance": abs(screen_error) <= 2.0
            })
            debug(f"{db:+3.1f}dB: Error={screen_error:+.1f}%")

        if results:
            errors = [r["screen_error"] for r in results]
            max_error = max(errors, key=abs)
        else:
            max_error = 0.0

        all_in_tolerance = all([r["in_tolerance"] for r in results]) if results else False

        out_cmd = get_command(gen_cfg, "output_state", channel=channel)
        if out_cmd: safe_write(gen, f"{out_cmd} OFF")

        output = {
            "success": True, "frequency": frequency, "initial_voltage": initial_voltage,
            "reference_voltage": reference_voltage, "reference_percent": reference_percent,
            "gain": gain, "max_error": max_error, "all_in_tolerance": all_in_tolerance,
            "results": results
        }
        print(json.dumps(output), flush=True)

    except Exception as e:
        print(json.dumps({"success": False, "error": str(e)}), flush=True)
    finally:
        try:
            if gen and gen_cfg:
                out_cmd = get_command(gen_cfg, "output_state", channel=channel if 'channel' in locals() else 1)
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