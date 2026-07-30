import sys
import json
import re
import math
from pathlib import Path
import pyvisa
import time


CONFIG_FOLDER = Path(__file__).parent / "instrument_configs"


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
                    self.cache[json_file.stem.upper()] = json.load(f)
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
    entry = cmds[name]
    cmd = entry.get("command", "") if isinstance(entry, dict) else entry
    if "channel" not in values:
        values["channel"] = 1
    consumed = set()
    for k, v in values.items():
        token = "{" + k + "}"
        if token in cmd:
            cmd = cmd.replace(token, str(v))
            consumed.add(k)
    if not cmd.rstrip().endswith("?"):
        leftover = [k for k in values if k not in consumed and k != "channel"]
        if leftover:
            cmd = f"{cmd} {values[leftover[0]]}"
    return cmd


def safe_query_float(inst, cmd):
    if not cmd:
        return None
    try:
        return float(inst.query(cmd).strip())
    except Exception:
        return None


def safe_write(inst, cmd):
    """Write a command and wait for *OPC? to confirm completion."""
    if not cmd:
        return False
    try:
        inst.write(cmd)
        # Wait for operation to complete (more reliable than sleep)
        try:
            inst.query("*OPC?")
        except:
            # If *OPC? not supported, fall back to sleep
            time.sleep(0.05)
        return True
    except Exception as e:
        debug(f"Write failed for '{cmd}': {e}")
        return False


def compute_step_values(reglage, v_meas, reglage_reference, v_reference,
                         reglage_previous, v_previous):
    if v_reference is None or v_meas is None:
        return {"gain_reel_total": None, "gain_reel_par_pas": None,
                "ecart_par_pas": None, "ecart_total": None}

    if v_meas <= 0 or v_reference <= 0:
        return {"gain_reel_total": None, "gain_reel_par_pas": None,
                "ecart_par_pas": None, "ecart_total": None}

    gain_reel_total = 20 * math.log10(v_meas / v_reference)

    gain_reel_par_pas = None
    ecart_par_pas = None
    if v_previous is not None and v_previous > 0 and reglage_previous is not None:
        gain_reel_par_pas = 20 * math.log10(v_meas / v_previous)
        nominal_step = reglage - reglage_previous
        ecart_par_pas = gain_reel_par_pas + nominal_step

    ecart_total = gain_reel_total + (reglage - reglage_reference)

    return {
        "gain_reel_total": round(gain_reel_total, 2),
        "gain_reel_par_pas": round(gain_reel_par_pas, 2) if gain_reel_par_pas is not None else None,
        "ecart_par_pas": round(ecart_par_pas, 2) if ecart_par_pas is not None else None,
        "ecart_total": round(ecart_total, 2),
    }


class GainMeasurement:
    """
    Main class for gain linearity measurement.
    Handles VISA communication and measurement logic.
    State is maintained internally between measurements.
    """
    
    def __init__(self):
        self.rm = None
        self.scope = None
        self.gen = None
        self.scope_cfg = None
        self.gen_cfg = None
        self.config_manager = ConfigManager(CONFIG_FOLDER)
        self.is_initialized = False
        
        # Measurement state (maintained between calls)
        self.reference_gain = None
        self.reference_voltage = None
        self.previous_gain = None
        self.previous_voltage = None
        
        # Burst trigger command (cached after first lookup)
        self.trigger_cmd = None

    def initialize(self, config):
        """Open VISA connections and configure the generator ONCE."""
        self.rm = pyvisa.ResourceManager()
        
        self.scope = self.rm.open_resource(config["scope_resource"])
        self.gen = self.rm.open_resource(config["generator_resource"])
        self.scope.timeout = 10000
        self.gen.timeout = 10000

        # Clear stale errors
        try:
            self.scope.write("*CLS")
            self.gen.write("*CLS")
        except:
            pass

        scope_idn = config.get("scope_idn") or self.scope.query("*IDN?").strip()
        gen_idn = config.get("generator_idn") or self.gen.query("*IDN?").strip()

        self.scope_cfg = config.get("scope_config")
        if not self.scope_cfg:
            scope_model = scope_idn.split(",")[1].strip()
            self.scope_cfg = self.config_manager.get(extract_prefix(scope_model))

        self.gen_cfg = config.get("generator_config")
        if not self.gen_cfg:
            gen_model = gen_idn.split(",")[1].strip()
            self.gen_cfg = self.config_manager.get(extract_prefix(gen_model))

        if self.scope_cfg is None:
            raise Exception(f"No configuration found for scope:\n{scope_idn}")
        if self.gen_cfg is None:
            raise Exception(f"No configuration found for generator:\n{gen_idn}")

        # Configure generator ONCE
        freq_hz = float(config["frequency_mhz"]) * 1e6
        vcc = float(config["tension_vcc"])

        # Get the trigger command for later use
        self.trigger_cmd = get_command(self.gen_cfg, "trigger")
        if not self.trigger_cmd:
            # Try alternative trigger commands
            self.trigger_cmd = get_command(self.gen_cfg, "burst_trigger_immediate")
        if not self.trigger_cmd:
            debug("WARNING: No trigger command found in config. Manual triggering required.")

        # Apply sine wave
        apply_cmd = get_command(self.gen_cfg, "apply_sine", value=f"{freq_hz},{vcc},0,0")
        if apply_cmd:
            safe_write(self.gen, apply_cmd)

        # --- BURST CONFIGURATION (SPECIFIC TO LDG) ---
        # 1. Turn burst ON
        burst_cmd = get_command(self.gen_cfg, "burst_state", state="ON")
        if burst_cmd:
            safe_write(self.gen, burst_cmd)
        else:
            debug("WARNING: 'burst_state' command not found")
        
        # 2. Set burst mode to TRIGgered
        burst_mode_cmd = get_command(self.gen_cfg, "burst_mode", mode="TRIGgered")
        if burst_mode_cmd:
            safe_write(self.gen, burst_mode_cmd)
        else:
            debug("WARNING: 'burst_mode' command not found")
        
        # 3. Set number of cycles to 11
        burst_ncycles_cmd = get_command(self.gen_cfg, "burst_ncycles", ncycles=11)
        if burst_ncycles_cmd:
            safe_write(self.gen, burst_ncycles_cmd)
        else:
            debug("WARNING: 'burst_ncycles' command not found")
        
        # 4. Set trigger source to EXTernal (updated from MANUAL)
        burst_trigger_cmd = get_command(self.gen_cfg, "burst_trigger_source", source="EXTernal")
        if burst_trigger_cmd:
            safe_write(self.gen, burst_trigger_cmd)
        else:
            debug("WARNING: 'burst_trigger_source' command not found")

        # 5. Turn output ON
        out_cmd = get_command(self.gen_cfg, "output_state", state="ON")
        if out_cmd:
            safe_write(self.gen, out_cmd)

        debug(f"Generator configured: {freq_hz/1e6:.2f} MHz, {vcc:.3f}V, BURST 11 cycles, EXTernal trigger")
        debug(f"Trigger command: {self.trigger_cmd if self.trigger_cmd else 'EXTernal (from source)'}")

        time.sleep(0.5)
        self.is_initialized = True
        
        # Reset state
        self.reference_gain = None
        self.reference_voltage = None
        self.previous_gain = None
        self.previous_voltage = None

    def trigger_burst(self):
        """Send trigger command to fire the burst."""
        if not self.trigger_cmd:
            debug("No trigger command available - waiting for EXTernal trigger")
            return False
        
        try:
            safe_write(self.gen, self.trigger_cmd)
            debug(f"Burst triggered: {self.trigger_cmd}")
            return True
        except Exception as e:
            debug(f"Failed to trigger burst: {e}")
            return False

    def measure(self, reglage, request_id=None):
        """
        Perform a single measurement at the given gain setting.
        Maintains reference and previous values internally.
        """
        if not self.is_initialized:
            raise Exception("Measurement not initialized. Call initialize() first.")

        # --- 1. TRIGGER THE BURST ---
        # If trigger command is available, fire it before measuring
        if self.trigger_cmd:
            self.trigger_burst()
        else:
            debug(f"Waiting for EXTernal trigger for gain {reglage} dB...")
        
        # --- 2. WAIT FOR BURST TO COMPLETE ---
        # Wait for the burst to finish before measuring
        # 11 cycles at frequency f: time = 11 / f
        # Add some margin (20%)
        freq_hz = float(self.scope_cfg.get("frequency", 5e6))  # Get from config if available
        if freq_hz:
            burst_duration = 11 / freq_hz  # seconds
            wait_time = min(max(burst_duration * 1.2, 0.001), 0.1)  # Between 1ms and 100ms
            debug(f"Waiting {wait_time*1000:.1f}ms for burst to complete")
            time.sleep(wait_time)
        else:
            # Fallback: fixed wait
            time.sleep(0.01)

        # --- 3. MEASURE VPP ---
        src_cmd = get_command(self.scope_cfg, "measurement_source", source="CHANnel1")
        if src_cmd:
            safe_write(self.scope, src_cmd)

        vpp_cmd = get_command(self.scope_cfg, "measurement_vpp")
        v_meas = safe_query_float(self.scope, vpp_cmd)

        if v_meas is None:
            raise Exception(f"Impossible de mesurer Vpp pour le gain {reglage} dB")

        computed = compute_step_values(
            reglage=float(reglage),
            v_meas=v_meas,
            reglage_reference=self.reference_gain,
            v_reference=self.reference_voltage,
            reglage_previous=self.previous_gain,
            v_previous=self.previous_voltage,
        )

        result = {
            "success": True,
            "reglage_gain": reglage,
            "attenuateur_externe": reglage - 10,
            "tension_mesuree": v_meas,
        }
        result.update(computed)

        if request_id is not None:
            result["request_id"] = request_id

        # Update state for next measurement
        if self.reference_gain is None:
            self.reference_gain = reglage
            self.reference_voltage = v_meas
        self.previous_gain = reglage
        self.previous_voltage = v_meas

        return result

    def close(self):
        """Close VISA connections and clean up."""
        # Turn off generator
        try:
            if self.gen and self.gen_cfg:
                out_cmd = get_command(self.gen_cfg, "output_state", state="OFF")
                if out_cmd:
                    safe_write(self.gen, out_cmd)
        except:
            pass

        try:
            if self.scope:
                self.scope.close()
        except:
            pass
        try:
            if self.gen:
                self.gen.close()
        except:
            pass
        try:
            if self.rm:
                self.rm.close()
        except:
            pass
        self.is_initialized = False
        self.reference_gain = None
        self.reference_voltage = None
        self.previous_gain = None
        self.previous_voltage = None


def send_response(response):
    """Send a JSON response to stdout (with newline)."""
    print(json.dumps(response), flush=True)


def main():
    """Main entry point - reads JSON commands from stdin."""
    measurement = GainMeasurement()
    all_results = []

    try:
        # Read initial configuration from stdin (first message)
        line = sys.stdin.readline()
        if not line:
            send_response({"success": False, "error": "No configuration received."})
            return

        try:
            config = json.loads(line.strip())
        except json.JSONDecodeError as e:
            send_response({"success": False, "error": f"Invalid JSON config: {str(e)}"})
            return

        # Initialize instruments
        measurement.initialize(config)

        # Send ready signal to VB
        send_response({"status": "ready"})

        # Main command loop
        while True:
            line = sys.stdin.readline()
            if not line:
                # EOF - VB closed the pipe
                break

            try:
                cmd = json.loads(line.strip())
            except json.JSONDecodeError as e:
                send_response({"success": False, "error": f"Invalid JSON command: {str(e)}"})
                continue

            command = cmd.get("command")
            request_id = cmd.get("request_id")

            if command == "measure":
                gain = cmd.get("gain")
                if gain is None:
                    send_response({"success": False, "error": "Missing 'gain' in measure command", "request_id": request_id})
                    continue

                # Perform the measurement
                try:
                    result = measurement.measure(gain, request_id)
                    all_results.append(result)
                    send_response(result)

                except Exception as e:
                    send_response({"success": False, "error": str(e), "request_id": request_id})

            elif command == "complete":
                # VB is done, send all results and exit
                send_response({"success": True, "results": all_results})
                break

            elif command == "cancel":
                send_response({"success": False, "error": "Cancelled by user", "request_id": request_id})
                break

            else:
                send_response({"success": False, "error": f"Unknown command: {command}", "request_id": request_id})

    except Exception as ex:
        send_response({"success": False, "error": str(ex)})

    finally:
        measurement.close()


if __name__ == "__main__":
    main()