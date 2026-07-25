import sys
import json
from pathlib import Path
import pyvisa
import time
import re


CONFIG_FOLDER = Path(__file__).parent / "instrument_configs"


def extract_prefix(model):
    """
    Turns a real instrument model number into its short family prefix,
    e.g. 'MSO5074' -> 'MSO5', 'DG4102' -> 'DG4', 'DS1054Z' -> 'DS1'.
    Rule: leading letters + the FIRST digit that follows them.
    """
    m = re.match(r'^([A-Za-z]+\d)', model)
    return m.group(1).upper() if m else model.upper()


def load_config_by_prefix(prefix):
    """Load a config file by its prefix name (e.g., 'MSO5' -> MSO5.json)"""
    if not CONFIG_FOLDER.exists():
        return None
    
    for json_file in CONFIG_FOLDER.glob("*.json"):
        if json_file.stem.upper() == prefix:
            try:
                with open(json_file, "r", encoding="utf-8") as f:
                    return json.load(f)
            except Exception:
                return None
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


def analyze_waveform(waveform, x_inc):
    """
    Analyse la forme d'onde pour extraire:
    - V50 = Vcc (tension d'alimentation, valeur max en valeur absolue)
    - td = temps de délai pour atteindre Vcc/2
    - Vr = pourcentage résiduel (tension résiduelle / Vcc * 100)
    """
    if not waveform or len(waveform) < 10:
        return None, None, None
    
    # Trouver la valeur maximale en valeur absolue (supporte les impulsions négatives)
    # V50 = Vcc = valeur absolue maximale
    vcc = max(waveform, key=abs)
    v50 = vcc
    
    # Seuil à 50% de Vcc pour le délai
    v_50_percent = vcc * 0.5
    
    # Trouver l'index où le signal atteint 50% de Vcc
    idx_50 = -1
    for i, val in enumerate(waveform):
        if abs(val) >= abs(v_50_percent):
            idx_50 = i
            break
    
    # Calculer td (temps de délai) - temps pour atteindre 50% de Vcc
    td = idx_50 * x_inc * 1e9 if idx_50 != -1 else 0  # en nanosecondes
    
    # Calculer Vr (pourcentage résiduel) - prendre la moyenne des derniers 50 points
    if len(waveform) > 50:
        steady_state = sum(waveform[-50:]) / 50
        vr = (abs(steady_state) / abs(vcc)) * 100 if vcc != 0 else 0
    else:
        vr = 0
    
    return v50, td, vr


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

    scope = None
    gen = None

    try:
        # Open connections
        scope = rm.open_resource(payload["scope_resource"])
        gen = rm.open_resource(payload["generator_resource"])

        scope.timeout = 10000  # 10 second timeout
        gen.timeout = 10000

        # Get instrument IDs with validation
        scope_idn = scope.query("*IDN?").strip()
        gen_idn = gen.query("*IDN?").strip()

        print(f"DEBUG: Scope IDN: {scope_idn}")
        print(f"DEBUG: Generator IDN: {gen_idn}")

        # Validate IDN format and extract model
        scope_parts = scope_idn.split(",")
        if len(scope_parts) < 2:
            raise Exception(f"Invalid IDN returned by scope (expected comma-separated fields):\n{scope_idn}")

        gen_parts = gen_idn.split(",")
        if len(gen_parts) < 2:
            raise Exception(f"Invalid IDN returned by generator (expected comma-separated fields):\n{gen_idn}")

        # Load configs based on prefix from IDN
        scope_model = scope_parts[1].strip()
        scope_prefix = extract_prefix(scope_model)
        scope_cfg = load_config_by_prefix(scope_prefix)
        print(f"DEBUG: Scope model '{scope_model}' -> prefix '{scope_prefix}'")
        if scope_cfg:
            print(f"DEBUG: Found scope config for prefix '{scope_prefix}'")
        else:
            print(f"DEBUG: No config found for scope prefix '{scope_prefix}'")

        gen_model = gen_parts[1].strip()
        gen_prefix = extract_prefix(gen_model)
        gen_cfg = load_config_by_prefix(gen_prefix)
        print(f"DEBUG: Generator model '{gen_model}' -> prefix '{gen_prefix}'")
        if gen_cfg:
            print(f"DEBUG: Found generator config for prefix '{gen_prefix}'")
        else:
            print(f"DEBUG: No config found for generator prefix '{gen_prefix}'")

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

                # 11. Analyze waveform
                if waveform and len(waveform) > 10:
                    # Get time increment for time calculations
                    try:
                        x_inc_cmd = get_command(scope_cfg, "waveform_x_increment")
                        if x_inc_cmd:
                            x_inc = float(scope.query(x_inc_cmd).strip())
                        else:
                            x_inc = 0.001
                    except Exception:
                        x_inc = 0.001  # Default fallback (1 ns per point)

                    # Analyze waveform (supports both positive and negative pulses)
                    v50, td_from_wave, vr = analyze_waveform(waveform, x_inc)
                    
                    # Use td from waveform analysis if SCPI measurement failed
                    if td is None and td_from_wave is not None:
                        td = td_from_wave
                    
                    result["v50_meas"] = v50
                    result["vcc"] = v50
                    result["td"] = td
                    result["tr"] = tr
                    result["vr"] = vr
                    result["x_increment"] = x_inc
                    
                    # Store reduced waveform data (only send points if needed)
                    # For performance, you can decide to send only every Nth point
                    # or limit the total number of points
                    MAX_WAVEFORM_POINTS = 2000
                    if len(waveform) > MAX_WAVEFORM_POINTS:
                        # Decimate: take every Nth point
                        step = len(waveform) // MAX_WAVEFORM_POINTS
                        if step < 1:
                            step = 1
                        result["waveform_points"] = waveform[::step]
                    else:
                        result["waveform_points"] = waveform
                else:
                    result["v50_meas"] = None
                    result["vcc"] = None
                    result["td"] = td
                    result["tr"] = tr
                    result["vr"] = None
                    result["x_increment"] = None
                    result["waveform_points"] = []

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