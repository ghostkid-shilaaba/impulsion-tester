import sys
import json
from pathlib import Path
import pyvisa
import time


CONFIG_FOLDER = Path(__file__).parent / "instrument_configs"


def load_configs():
    configs = []

    if CONFIG_FOLDER.exists():
        for file in CONFIG_FOLDER.glob("*.json"):
            try:
                with open(file, "r", encoding="utf-8") as f:
                    configs.append(json.load(f))
            except Exception:
                pass

    return configs


def find_config(idn, inst_type, configs):
    idn = idn.upper()

    for cfg in configs:
        if cfg.get("type", "").lower() != inst_type.lower():
            continue

        for model in cfg.get("model_match", []):
            if model.upper() in idn:
                return cfg

    return None


def get_command(cfg, name, **values):
    """
    Get the command string from the config and replace placeholders with values.
    Uses the long descriptive command names from the config.
    """
    if cfg is None:
        return None

    cmds = cfg.get("commands", {})
    if name not in cmds:
        return None

    # Get the command - could be string or object
    cmd_obj = cmds[name]
    
    # If it's a dict, extract the command string
    if isinstance(cmd_obj, dict):
        cmd = cmd_obj.get("command", "")
    else:
        cmd = cmd_obj

    # Default channel to 1 if not specified
    if "channel" not in values:
        values["channel"] = 1

    for k, v in values.items():
        cmd = cmd.replace("{" + k + "}", str(v))

    return cmd


def check_error(inst):
    """Check for instrument errors using :SYST:ERR? query"""
    try:
        return inst.query(":SYST:ERR?").strip()
    except Exception:
        return "Unknown"


def execute_batch():
    if len(sys.argv) < 2:
        print(json.dumps({
            "success": False,
            "error": "Missing payload."
        }))
        return

    try:
        payload = json.loads(sys.argv[1])
    except json.JSONDecodeError as e:
        print(json.dumps({
            "success": False,
            "error": f"Invalid JSON payload: {str(e)}"
        }))
        return

    rm = pyvisa.ResourceManager()
    configs = load_configs()

    scope = None
    gen = None

    try:
        # Open connections
        scope = rm.open_resource(payload["scope_resource"])
        gen = rm.open_resource(payload["generator_resource"])

        scope.timeout = 10000  # 10 second timeout
        gen.timeout = 10000

        # Get instrument IDs
        scope_idn = scope.query("*IDN?").strip()
        gen_idn = gen.query("*IDN?").strip()

        # Find configurations
        scope_cfg = find_config(scope_idn, "oscilloscope", configs)
        gen_cfg = find_config(gen_idn, "generator", configs)

        if scope_cfg is None:
            raise Exception(f"No configuration found for scope:\n{scope_idn}")

        if gen_cfg is None:
            raise Exception(f"No configuration found for generator:\n{gen_idn}")

        results = []

        # Process each impulse
        for imp in payload["impulses"]:
            result = {
                "index": imp["index"],
                "success": True,
                "amplitude": imp["amplitude"],
                "damping": imp["damping"],
                "prf": imp["prf"]
            }

            try:
                # 1. Set amplitude using voltage_amplitude command
                amp_cmd = get_command(gen_cfg, "voltage_amplitude", amplitude=imp["amplitude"])
                if amp_cmd:
                    gen.write(amp_cmd)
                    time.sleep(0.1)

                # 2. Set PRF (frequency) using frequency_fixed command
                freq_cmd = get_command(gen_cfg, "frequency_fixed", prf=imp["prf"])
                if freq_cmd:
                    gen.write(freq_cmd)
                    time.sleep(0.1)

                # 3. Set damping using pulse_duty command
                damp_cmd = get_command(gen_cfg, "pulse_duty", damping=imp["damping"])
                if damp_cmd:
                    gen.write(damp_cmd)
                    time.sleep(0.1)

                # 4. Turn output ON using output_state command
                out_cmd = get_command(gen_cfg, "output_state", state="ON")
                if out_cmd:
                    gen.write(out_cmd)
                    time.sleep(0.5)

                # 5. Check for generator errors
                err = check_error(gen)
                if not err.startswith("0"):
                    raise Exception(f"Generator error: {err}")

                # 6. Set measurement source using measurement_source from scope config
                meas_source_cmd = get_command(scope_cfg, "measurement_source", source="CHANnel1")
                if meas_source_cmd:
                    scope.write(meas_source_cmd)
                    time.sleep(0.1)

                # 7. Get td (delay time) using measurement_delay from scope config
                td = None
                delay_cmd = get_command(scope_cfg, "measurement_delay")
                if delay_cmd:
                    try:
                        td_raw = scope.query(delay_cmd)
                        if td_raw:
                            td = float(td_raw.strip()) * 1e9
                    except Exception:
                        td = None

                # 8. Get tr (rise time) using measurement_rise from scope config
                tr = None
                rise_cmd = get_command(scope_cfg, "measurement_rise")
                if rise_cmd:
                    try:
                        tr_raw = scope.query(rise_cmd)
                        if tr_raw:
                            tr = float(tr_raw.strip()) * 1e9
                    except Exception:
                        tr = None

                # 9. Set waveform format using waveform_format from scope config
                format_cmd = get_command(scope_cfg, "waveform_format", format="ASCII")
                if format_cmd:
                    try:
                        scope.write(format_cmd)
                    except Exception:
                        pass

                # 10. Capture waveform using get_waveform from scope config
                wave_cmd = get_command(scope_cfg, "get_waveform")
                waveform = []

                if wave_cmd:
                    for retry in range(3):
                        try:
                            raw = scope.query(wave_cmd)
                            if raw:
                                break
                        except Exception:
                            if retry == 2:
                                raise
                            time.sleep(0.1)

                    try:
                        if "," in raw:
                            waveform = [float(x.strip()) for x in raw.split(",") if x.strip()]
                        else:
                            waveform = [float(x) for x in raw.replace(",", " ").split() if x]
                    except Exception:
                        waveform = []

                result["waveform_points"] = waveform

                # 11. Get V50 from waveform (peak value)
                if waveform and len(waveform) > 10:
                    v50 = max(waveform)
                    result["v50_meas"] = v50
                    result["vcc"] = v50
                else:
                    result["v50_meas"] = None
                    result["vcc"] = None

                # 12. Store td and tr
                result["td"] = td
                result["tr"] = tr

                # 13. Calculate Vr (residual percentage) from waveform
                if waveform and len(waveform) > 50:
                    vcc_val = result.get("vcc", 0)
                    if vcc_val and vcc_val > 0:
                        steady_state = sum(waveform[-50:]) / 50
                        vr = (steady_state / vcc_val) * 100
                        result["vr"] = vr
                    else:
                        result["vr"] = 0
                else:
                    result["vr"] = 0

            except Exception as ex:
                result["success"] = False
                result["error"] = str(ex)

            results.append(result)

        # Output results
        print(json.dumps({
            "success": True,
            "scope": scope_idn,
            "generator": gen_idn,
            "results": results
        }))

    except Exception as ex:
        print(json.dumps({
            "success": False,
            "error": str(ex)
        }))

    finally:
        try:
            if scope:
                scope.close()
        except Exception:
            pass

        try:
            if gen:
                gen.close()
        except Exception:
            pass

        try:
            rm.close()
        except Exception:
            pass


if __name__ == "__main__":
    execute_batch()