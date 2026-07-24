import json
import re
import pyvisa
from pathlib import Path


def extract_prefix(model):
    """
    Turns a real instrument model number into its short family prefix,
    e.g. 'MSO5074' -> 'MSO5', 'DG4102' -> 'DG4', 'DS1054Z' -> 'DS1'.
    Rule: leading letters + the first digit that follows them.
    """
    m = re.match(r'^[A-Za-z]+\d+', model)
    return m.group(1).upper() if m else model.upper()


def get_devices():
    rm = pyvisa.ResourceManager()
    try:
        instruments = rm.list_resources()
    except Exception:
        print(json.dumps({}))
        return

    config_folder = Path(__file__).parent / "instrument_configs"
    detected_devices = {}

    for inst in instruments:
        try:
            with rm.open_resource(inst) as temp_inst:
                idn = temp_inst.query("*IDN?").strip()
                print(f"DEBUG: Found instrument: {idn}")

                # Split the IDN string: MANUFACTURER,MODEL,SERIAL,FIRMWARE
                parts = [p.strip() for p in idn.split(",")]
                if len(parts) < 2:
                    print(f"DEBUG: Could not parse IDN (expected 4 comma-separated fields): {idn}")
                    continue

                manufacturer_from_idn = parts[0]
                model = parts[1]

                match = re.match(r'^[A-Za-z]+\d+', model)
                if not match:
                    print(f"DEBUG: Could not extract prefix from model '{model}'")
                    continue
                prefix = extract_prefix(model)

                print(f"DEBUG: Model '{model}' -> prefix '{prefix}' -> looking for '{prefix}.json'")

                # Look for a config file named exactly '<prefix>.json' (case-insensitive)
                cfg = None
                matched_filename = None
                if config_folder.exists():
                    for json_file in config_folder.glob("*.json"):
                        if json_file.stem.upper() == prefix:
                            try:
                                with open(json_file, "r", encoding="utf-8") as f:
                                    cfg = json.load(f)
                                matched_filename = json_file.name
                            except Exception as e:
                                print(f"DEBUG: Failed to load {json_file.name}: {e}")
                            break

                if cfg is None:
                    print(f"DEBUG: No config file found for prefix '{prefix}' "
                          f"(looked for '{prefix}.json' in {config_folder})")
                    continue

                meta = cfg.get("meta", {})
                inst_type = meta.get("deviceType", "unknown")
                manufacturer = meta.get("manufacturer", manufacturer_from_idn)

                detected_devices[inst_type] = {
                    "idn": idn,
                    "resource": inst,
                    "manufacturer": manufacturer,
                    "prefix": prefix,
                    "config_file": matched_filename
                }
                print(f"DEBUG: Matched! {inst_type} = {idn} (config: {matched_filename})")

        except Exception as e:
            print(f"DEBUG: Error with {inst}: {e}")
            pass

    try:
        rm.close()
    except Exception:
        pass

    print(json.dumps(detected_devices))


if __name__ == "__main__":
    get_devices()