import sys
import json
from pathlib import Path
import pyvisa
import time
import re
import os


CONFIG_FOLDER = Path(__file__).parent / "instrument_configs"
CANCEL_FLAG = os.path.join(os.environ.get("TEMP", "/tmp"), "impulsion_cancel.flag")


def debug(msg):
    print(f"DEBUG: {msg}", file=sys.stderr)


def extract_prefix(model):
    m = re.match(r'^([A-Za-z]+\d)', model)
    return m.group(1).upper() if m else model.upper()


class ConfigManager:
    """Cache configuration files to avoid repeated disk reads"""

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
            except Exception as e:
                debug(f"Failed to load {json_file.name}: {e}")

    def get(self, prefix):
        return self.cache.get(prefix)


def get_command(cfg, name, **values):
    if cfg is None:
        return None
    cmds = cfg.get("commands", {})
    if name not in cmds:
        return None
    cmd_obj = cmds[name]
    cmd = cmd_obj.get("command", "") if isinstance(cmd_obj, dict) else cmd_obj

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


def check_error(inst):
    try:
        return inst.query(":SYST:ERR?").strip()
    except Exception:
        return "Unknown"


def safe_query_float(inst, cmd):
    """Query a numeric SCPI response, return None on any failure."""
    if not cmd:
        return None
    try:
        raw = inst.query(cmd).strip()
        return float(raw)
    except Exception:
        return None


def capture_screenshot(scope, scope_cfg, output_dir, index):
    """
    Capture the oscilloscope's screen image via :DISPlay:DATA? and save
    it to output_dir as BMP. Uses pyvisa's query_binary_values, which
    handles the IEEE 488.2 block header and chunked reads for large
    transfers more robustly than a manual read_raw() + manual parse.
    Returns the saved file path, or None on any failure (never raises --
    a failed screenshot shouldn't abort the whole measurement).
    """
    cmd = get_command(scope_cfg, "display_data")
    if not cmd:
        return None
    try:
        image_bytes = scope.query_binary_values(
            cmd, datatype='B', container=bytes
        )
        if not image_bytes:
            return None

        os.makedirs(output_dir, exist_ok=True)
        # Rigol's :DISPlay:DATA? returns BMP data by default on MSO5000.
        file_path = os.path.join(output_dir, f"impulsion_{index}_scope.bmp")
        with open(file_path, "wb") as f:
            f.write(image_bytes)
        return file_path
    except Exception as e:
        debug(f"Screenshot capture failed for impulsion {index}: {e}")
        return None


def execute_batch():
    if len(sys.argv) < 2:
        print(json.dumps({"success": False, "error": "Missing payload file path."}))
        return

    try:
        with open(sys.argv[1], "r", encoding="utf-8") as f:
            payload = json.load(f)
    except Exception as e:
        print(json.dumps({"success": False, "error": f"Failed to load payload file: {str(e)}"}))
        return

    try:
        if os.path.exists(CANCEL_FLAG):
            os.remove(CANCEL_FLAG)
    except Exception:
        pass

    config_manager = ConfigManager(CONFIG_FOLDER)
    rm = pyvisa.ResourceManager()

    scope = None

    try:
        # -------------------------------------------------------------
        # Impulsion d'émission does NOT use the function generator.
        # The flaw detector's own pulser fires into the oscilloscope
        # (through a 20dB external attenuator, per the safety notice on
        # the test screen). Only the scope is opened/used here.
        # -------------------------------------------------------------
        scope = rm.open_resource(payload["scope_resource"])
        scope.timeout = 10000

        scope_idn = payload.get("scope_idn")
        if not scope_idn:
            scope_idn = scope.query("*IDN?").strip()
            debug(f"Scope IDN (queried): {scope_idn}")
        else:
            debug(f"Scope IDN (from payload): {scope_idn}")

        scope_cfg = payload.get("scope_config")
        if not scope_cfg:
            scope_parts = scope_idn.split(",")
            if len(scope_parts) < 2:
                raise Exception(f"Invalid IDN: {scope_idn}")
            scope_model = scope_parts[1].strip()
            scope_prefix = extract_prefix(scope_model)
            scope_cfg = config_manager.get(scope_prefix)
            debug(f"Loaded scope config for prefix '{scope_prefix}'")
        else:
            debug("Using scope config from payload")

        if scope_cfg is None:
            raise Exception(f"No configuration found for scope:\n{scope_idn}")

        results = []
        total_impulses = len(payload["impulses"])

        for idx, imp in enumerate(payload["impulses"]):
            if os.path.exists(CANCEL_FLAG):
                debug("Cancellation requested by user")
                raise Exception("Acquisition cancelled by user")

            print(json.dumps({"progress": idx + 1}), file=sys.stderr)

            # amplitude/damping/prf are what the TECHNICIAN set on the
            # physical flaw detector's dials -- informational only,
            # recorded for the constat, never sent to any instrument.
            result = {
                "index": imp["index"],
                "success": True,
                "amplitude_set": imp["amplitude"],
                "damping_set": imp["damping"],
                "prf_set": imp["prf"],
            }

            try:
                # 1. Point the scope's measurement source at the channel
                #    the flaw detector's T/R output is physically wired to.
                src_cmd = get_command(scope_cfg, "measurement_source", source="CHANnel1")
                if src_cmd:
                    scope.write(src_cmd)
                    time.sleep(0.1)

                # 2. V50 = Vcc measured at the oscilloscope (peak-to-peak
                #    voltage of the flaw detector's own pulse).
                vpp_cmd = get_command(scope_cfg, "measurement_vpp")
                vcc = safe_query_float(scope, vpp_cmd)

                # 3. td = pulse duration at the 50% threshold. The flaw
                #    detector's emission pulse is negative-going (per the
                #    constat reference graphs), so this is the NEGATIVE
                #    pulse width. If your bench setup captures a
                #    positive-going pulse instead, swap to "measurement_pwidth".
                td_cmd = get_command(scope_cfg, "measurement_nwidth")
                td_raw = safe_query_float(scope, td_cmd)
                td = td_raw * 1e9 if td_raw is not None else None  # seconds -> ns

                # 4. tr = fall time (customer requested fall time instead of rise time)
                tr_cmd = get_command(scope_cfg, "measurement_fall")
                tr_raw = safe_query_float(scope, tr_cmd)
                tr = tr_raw * 1e9 if tr_raw is not None else None  # seconds -> ns

                # NOTE: Vreverb (Vr) is intentionally NOT computed. Per the
                # cahier des charges: "Supprimer la mesure Vreverb qui n'est
                # plus demandée et V50 devient égale à Vcc mesurée à
                # l'oscillo." This measurement has been dropped entirely.

                result["v50_meas"] = vcc
                result["vcc"] = vcc
                result["td"] = td
                result["tr"] = tr

                # 4b. Capture the oscilloscope's screen image, per the
                # cahier des charges: "Enregistrement de l'image écran
                # de l'oscillo... (boucle de test maxi : 8)" -- one
                # screenshot per impulse in the loop.
                screenshot_dir = payload.get("screenshot_dir") or os.path.join(
                    os.environ.get("TEMP", "/tmp"), "impulsion_screenshots")
                screenshot_path = capture_screenshot(scope, scope_cfg, screenshot_dir, imp["index"])
                result["screenshot_path"] = screenshot_path

                # 5. Capture the raw waveform too, purely for the results
                #    screen's chart -- not used for any of the calculations
                #    above, since those now come from real scope commands.
                waveform = []
                x_increment = None
                fmt_cmd = get_command(scope_cfg, "waveform_format", format="ASCII")
                if fmt_cmd:
                    scope.write(fmt_cmd)
                    time.sleep(0.1)

                xinc_cmd = get_command(scope_cfg, "waveform_x_increment")
                x_increment = safe_query_float(scope, xinc_cmd)

                wave_cmd = get_command(scope_cfg, "get_waveform")
                if wave_cmd:
                    raw = None
                    for retry in range(2):
                        try:
                            raw = scope.query(wave_cmd)
                            if raw:
                                break
                        except Exception:
                            if retry == 1:
                                raise
                            time.sleep(0.1)
                    if raw:
                        try:
                            if "," in raw:
                                waveform = [float(x.strip()) for x in raw.split(",") if x.strip()]
                            else:
                                waveform = [float(x) for x in raw.replace(",", " ").split() if x]
                        except Exception:
                            waveform = []

                result["x_increment"] = x_increment
                MAX_WAVEFORM_POINTS = 2000
                if len(waveform) > MAX_WAVEFORM_POINTS:
                    step = max(1, len(waveform) // MAX_WAVEFORM_POINTS)
                    result["waveform_points"] = waveform[::step]
                else:
                    result["waveform_points"] = waveform

                # 6. Check for scope errors after the sequence.
                err = check_error(scope)
                if err and not err.startswith("0"):
                    result["scope_warning"] = err

            except Exception as ex:
                result["success"] = False
                result["error"] = str(ex)

            results.append(result)

        try:
            if os.path.exists(CANCEL_FLAG):
                os.remove(CANCEL_FLAG)
        except Exception:
            pass

        print(json.dumps({
            "success": True,
            "scope": scope_idn,
            "results": results
        }))

    except Exception as ex:
        print(json.dumps({"success": False, "error": str(ex)}))

    finally:
        try:
            if scope:
                scope.close()
        except Exception:
            pass
        try:
            rm.close()
        except Exception:
            pass
        try:
            if os.path.exists(CANCEL_FLAG):
                os.remove(CANCEL_FLAG)
        except Exception:
            pass


if __name__ == "__main__":
    execute_batch()