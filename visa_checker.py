import sys
import json
import re
import pyvisa
from pathlib import Path
import time
from concurrent.futures import ThreadPoolExecutor, as_completed
import os

CACHE_FILE = os.path.join(os.environ.get("TEMP", "/tmp"), "visa_devices.json")
CACHE_TIMEOUT = 300  # 5 minutes


def debug(msg):
    print(f"DEBUG: {msg}", file=sys.stderr)


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
            debug(f"Config folder does not exist: {self.config_folder}")
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
        return None
    entry = cmds[name]
    cmd = entry.get("command", "") if isinstance(entry, dict) else entry
    
    for k, v in values.items():
        token = "{" + k + "}"
        if token in cmd:
            cmd = cmd.replace(token, str(v))
    
    return cmd


def apply_base_config(inst, cfg):
    """
    Apply base configuration to an instrument from the baseconfig section.
    For oscilloscope: sets trigger, timebase, channel settings
    For generator: SKIPPED - generator is configured by the test script itself
    """
    base = cfg.get("baseconfig", {})
    if not base:
        debug("No baseconfig found - skipping")
        return False
    
    meta = cfg.get("meta", {})
    inst_type = meta.get("deviceType", "unknown")
    debug(f"Applying base config for {inst_type}")
    
    try:
        if inst_type == "oscilloscope":
            # Channel settings
            channel_display_cmd = get_command(cfg, "channel_display", channel=1)
            if channel_display_cmd:
                inst.write(f"{channel_display_cmd} ON")
                time.sleep(0.05)
            
            channel_scale_cmd = get_command(cfg, "channel_scale", channel=1)
            scale_val = base.get("channel_scale", 0.5)
            if channel_scale_cmd:
                inst.write(f"{channel_scale_cmd} {scale_val}")
                time.sleep(0.05)
            
            channel_impedance_cmd = get_command(cfg, "channel_impedance", channel=1)
            imp_val = base.get("channel_impedance", 50)
            if channel_impedance_cmd:
                inst.write(f"{channel_impedance_cmd} {imp_val}")
                time.sleep(0.05)
            
            # Trigger settings
            trigger_source_cmd = get_command(cfg, "trigger_source")
            src_val = base.get("trigger_source", "CH1")
            if trigger_source_cmd:
                inst.write(f"{trigger_source_cmd} {src_val}")
                time.sleep(0.05)
            
            trigger_slope_cmd = get_command(cfg, "trigger_slope")
            slope_val = base.get("trigger_slope", "NEGative")
            if trigger_slope_cmd:
                inst.write(f"{trigger_slope_cmd} {slope_val}")
                time.sleep(0.05)
            
            trigger_level_cmd = get_command(cfg, "trigger_level")
            level_val = base.get("trigger_level", 0.5)
            if trigger_level_cmd:
                inst.write(f"{trigger_level_cmd} {level_val}")
                time.sleep(0.05)
            
            # Timebase
            timebase_cmd = get_command(cfg, "timebase_scale")
            tb_val = base.get("timebase", 2e-8)
            if timebase_cmd:
                inst.write(f"{timebase_cmd} {tb_val}")
                time.sleep(0.05)
            
            debug("Oscilloscope base config applied")
            return True
            
        elif inst_type == "generator":
            # SKIP generator configuration - it will be configured by the test script
            # (linearite_gain.py for LDG, lva_measurement.py for LVA, etc.)
            debug("Skipping generator base config - will be configured by test script")
            return True
        
        return False
    except Exception as e:
        debug(f"Failed to apply base config: {e}")
        return False


def scan_resource(resource_name, rm, config_manager):
    try:
        with rm.open_resource(resource_name) as temp_inst:
            temp_inst.timeout = 3000
            idn = temp_inst.query("*IDN?").strip()
            debug(f"Found instrument: {idn}")

            parts = [p.strip() for p in idn.split(",")]
            if len(parts) < 2:
                debug(f"Could not parse IDN: {idn}")
                return None

            manufacturer_from_idn = parts[0]
            model = parts[1]
            prefix = extract_prefix(model)
            debug(f"Model '{model}' -> prefix '{prefix}'")

            cfg = config_manager.get(prefix)
            if cfg is None:
                debug(f"No config file found for prefix '{prefix}'")
                return None

            meta = cfg.get("meta", {})
            inst_type = meta.get("deviceType", "unknown")
            manufacturer = meta.get("manufacturer", manufacturer_from_idn)

            debug(f"Matched! {inst_type} = {idn}")

            return {
                "type": inst_type,
                "idn": idn,
                "resource": resource_name,
                "manufacturer": manufacturer,
                "prefix": prefix,
                "config": cfg
            }

    except pyvisa.errors.VisaIOError as e:
        debug(f"VISA error with {resource_name}: {e}")
        return None
    except Exception as e:
        debug(f"Error with {resource_name}: {e}")
        return None


def is_cache_valid():
    if not os.path.exists(CACHE_FILE):
        return False
    try:
        file_time = os.path.getmtime(CACHE_FILE)
        if (time.time() - file_time) < CACHE_TIMEOUT:
            with open(CACHE_FILE, "r", encoding="utf-8") as f:
                json.load(f)
            return True
    except Exception as e:
        debug(f"Cache validation failed: {e}")
    return False


def load_from_cache():
    try:
        with open(CACHE_FILE, "r", encoding="utf-8") as f:
            return json.load(f)
    except Exception as e:
        debug(f"Failed to load cache: {e}")
        return None


def save_to_cache(devices):
    try:
        with open(CACHE_FILE, "w", encoding="utf-8") as f:
            json.dump(devices, f, indent=2)
        debug(f"Saved to cache: {CACHE_FILE}")
        return True
    except Exception as e:
        debug(f"Failed to save cache: {e}")
        return False


def delete_cache():
    try:
        if os.path.exists(CACHE_FILE):
            os.remove(CACHE_FILE)
            debug("Cache deleted")
            return True
    except Exception as e:
        debug(f"Failed to delete cache: {e}")
    return False


def run_detection(force_refresh=False):
    if not force_refresh and is_cache_valid():
        cached = load_from_cache()
        if cached:
            debug("Using cached detection results")
            return cached
    
    if os.path.exists(CACHE_FILE):
        delete_cache()
    
    debug("Running fresh detection...")
    
    rm = pyvisa.ResourceManager()
    rm.timeout = 1000

    try:
        all_resources = rm.list_resources()
        debug(f"Found {len(all_resources)} total resources")

        instruments = [r for r in all_resources if "USB" in r or "TCPIP" in r]
        if not instruments:
            debug("No USB/TCPIP resources found, falling back to all resources")
            instruments = all_resources

        if len(instruments) == 0:
            debug("No VISA instruments detected.")
            try:
                rm.close()
            except:
                pass
            return {}

        debug(f"Scanning {len(instruments)} resources")

    except Exception as e:
        debug(f"Error listing resources: {e}")
        try:
            rm.close()
        except:
            pass
        return {}

    config_folder = Path(__file__).parent / "instrument_configs"
    config_manager = ConfigManager(config_folder)
    detected_devices = {}

    max_workers = min(6, len(instruments))
    debug(f"Using {max_workers} parallel workers")

    with ThreadPoolExecutor(max_workers=max_workers) as executor:
        futures = {
            executor.submit(scan_resource, resource, rm, config_manager): resource
            for resource in instruments
        }

        for future in as_completed(futures):
            resource = futures[future]
            try:
                result = future.result(timeout=10)
                if result:
                    inst_type = result["type"]
                    if inst_type not in detected_devices:
                        detected_devices[inst_type] = []
                    detected_devices[inst_type].append({
                        "idn": result["idn"],
                        "resource": result["resource"],
                        "manufacturer": result["manufacturer"],
                        "prefix": result["prefix"],
                        "config": result["config"]
                    })
                    debug(f"Added {inst_type}: {result['idn']}")

            except TimeoutError:
                debug(f"Timeout scanning {resource}")
            except Exception as e:
                debug(f"Error collecting result for {resource}: {e}")

    try:
        rm.close()
    except:
        pass

    # --- APPLY BASE CONFIG TO DETECTED INSTRUMENTS (oscilloscope only) ---
    for inst_type, devices in detected_devices.items():
        for device in devices:
            cfg = device.get("config")
            if cfg and inst_type == "oscilloscope":
                try:
                    debug(f"Applying base config to {inst_type} on {device['resource']}")
                    with pyvisa.ResourceManager() as temp_rm:
                        temp_inst = temp_rm.open_resource(device["resource"])
                        temp_inst.timeout = 3000
                        apply_base_config(temp_inst, cfg)
                        temp_inst.close()
                    debug(f"Base config applied to {device['resource']}")
                except Exception as e:
                    debug(f"Failed to apply base config to {device['resource']}: {e}")
            elif cfg and inst_type == "generator":
                debug(f"Skipping generator config for {device['resource']} - will be configured by test script")

    save_to_cache(detected_devices)
    
    debug(f"Detection complete. Found: {list(detected_devices.keys())}")
    return detected_devices


def get_devices():
    force_refresh = "--refresh" in sys.argv
    clear_cache = "--clear" in sys.argv
    
    if clear_cache:
        delete_cache()
        print(json.dumps({"status": "cache_cleared"}))
        return
    
    result = run_detection(force_refresh)
    print(json.dumps(result))


if __name__ == "__main__":
    get_devices()