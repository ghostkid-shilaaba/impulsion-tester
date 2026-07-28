import sys
import json
import re
import pyvisa
from pathlib import Path
import time
from concurrent.futures import ThreadPoolExecutor, as_completed


def debug(msg):
    """Print debug messages to stderr (won't break JSON parsing)"""
    print(f"DEBUG: {msg}", file=sys.stderr)


def extract_prefix(model):
    """
    Turns a real instrument model number into its short family prefix,
    e.g. 'MSO5074' -> 'MSO5', 'DG4102' -> 'DG4', 'DS1054Z' -> 'DS1'.
    Rule: leading letters + the FIRST digit that follows them.
    """
    m = re.match(r'^([A-Za-z]+\d)', model)
    return m.group(1).upper() if m else model.upper()


class ConfigManager:
    """Cache configuration files to avoid repeated disk reads"""

    def __init__(self, config_folder):
        self.config_folder = config_folder
        self.cache = {}
        self._load_all()

    def _load_all(self):
        """Load all config files at once into cache"""
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
        """Get config by prefix (O(1) lookup)"""
        return self.cache.get(prefix)

    def get_all(self):
        """Return all cached configs"""
        return self.cache


def get_command(cfg, name, **values):
    """Same helper used in visa_commander.py / linearite_gain.py, so
    autoscale reuses whatever command name is in that instrument's
    real config file instead of a hardcoded SCPI string."""
    if cfg is None:
        return None
    cmds = cfg.get("commands", {})
    if name not in cmds:
        return None
    entry = cmds[name]
    cmd = entry.get("command", "") if isinstance(entry, dict) else entry
    return cmd


def try_autoscale(inst, cfg):
    """
    Fires the oscilloscope's autoscale command (e.g. SYSTem:AUToscale on
    Rigol scopes) so the display shows an actual trace instead of a flat
    line, if any signal is present on the input at detection time.

    NOTE: autoscale needs a live signal to actually find something to
    scale to. If nothing is connected/outputting yet when detection
    runs, this may not produce a meaningful result -- in that case, the
    same command should also be called from visa_commander.py /
    linearite_gain.py right before the real measurement, once an
    actual signal (the flaw detector's pulse, or the generator's test
    signal) is genuinely present.

    Never raises -- a failed/skipped autoscale shouldn't break detection.
    """
    cmd = get_command(cfg, "auto_scale")
    if not cmd:
        debug("No 'auto_scale' command in this scope's config -- skipping.")
        return False

    try:
        original_timeout = inst.timeout
        inst.write(cmd)

        # Autoscale is slow on real hardware (can take several seconds
        # while it adjusts vertical/horizontal scale and trigger).
        # Give it a generous timeout for the *OPC? sync, then restore
        # the short scanning timeout afterward.
        inst.timeout = 8000
        try:
            inst.query("*OPC?")
        except Exception:
            # Some scopes don't respond well to *OPC? after AUToscale --
            # fall back to a fixed wait instead of failing outright.
            time.sleep(3)
        finally:
            inst.timeout = original_timeout

        debug(f"Autoscale command sent: {cmd}")
        return True
    except Exception as e:
        debug(f"Autoscale failed (non-fatal): {e}")
        return False


def scan_resource(resource_name, rm, config_manager):
    """
    Scan a single VISA resource - each thread opens its own connection.
    This is thread-safe because each thread has its own VISA session.
    """
    try:
        # Open resource in this thread
        with rm.open_resource(resource_name) as temp_inst:
            temp_inst.timeout = 1000  # 1 second timeout for scanning

            # Query IDN
            idn = temp_inst.query("*IDN?").strip()
            debug(f"Found instrument: {idn}")

            # Parse IDN: Manufacturer,Model,Serial,Version
            parts = [p.strip() for p in idn.split(",")]
            if len(parts) < 2:
                debug(f"Could not parse IDN (expected 4 comma-separated fields): {idn}")
                return None

            manufacturer_from_idn = parts[0]
            model = parts[1]

            # Extract prefix (e.g., MSO5074 -> MSO5)
            prefix = extract_prefix(model)
            debug(f"Model '{model}' -> prefix '{prefix}'")

            # Try to find config for this prefix
            cfg = config_manager.get(prefix)
            if cfg is None:
                debug(f"No config file found for prefix '{prefix}'")
                return None

            # Get device info from config
            meta = cfg.get("meta", {})
            inst_type = meta.get("deviceType", "unknown")
            manufacturer = meta.get("manufacturer", manufacturer_from_idn)

            debug(f"Matched! {inst_type} = {idn}")

            # If this is an oscilloscope, wake its display up with
            # autoscale so it isn't just showing a flat line.
            if inst_type == "oscilloscope":
                try_autoscale(temp_inst, cfg)

            return {
                "type": inst_type,
                "idn": idn,
                "resource": resource_name,
                "manufacturer": manufacturer,
                "prefix": prefix,
                "config": cfg  # Pass the full config to avoid re-reading later
            }

    except pyvisa.errors.VisaIOError as e:
        debug(f"VISA error with {resource_name}: {e}")
        return None
    except Exception as e:
        debug(f"Error with {resource_name}: {e}")
        return None


def get_devices():
    """
    Main function: detect VISA instruments and return JSON with configs.
    Uses parallel scanning for speed.
    """
    rm = pyvisa.ResourceManager()
    rm.timeout = 1000  # 1 second timeout for resource manager

    try:
        # Get all available resources
        all_resources = rm.list_resources()
        debug(f"Found {len(all_resources)} total resources")

        # Filter to only USB and TCPIP (skip serial/GPIB for speed)
        instruments = [r for r in all_resources if "USB" in r or "TCPIP" in r]
        if not instruments:
            debug("No USB/TCPIP resources found, falling back to all resources")
            instruments = all_resources

        # FIXED: Check if no instruments were found
        if len(instruments) == 0:
            debug("No VISA instruments detected.")
            print(json.dumps({}))
            try:
                rm.close()
            except Exception:
                pass
            return

        debug(f"Scanning {len(instruments)} resources")

    except Exception as e:
        debug(f"Error listing resources: {e}")
        print(json.dumps({}))
        try:
            rm.close()
        except Exception:
            pass
        return

    # Initialize config manager
    config_folder = Path(__file__).parent / "instrument_configs"
    config_manager = ConfigManager(config_folder)

    # Dictionary to store detected devices (supports multiple devices per type)
    detected_devices = {}

    # Parallel scanning using ThreadPoolExecutor
    # Each thread opens its own VISA connection (thread-safe)
    max_workers = min(6, len(instruments))  # Don't create more threads than needed
    debug(f"Using {max_workers} parallel workers")

    with ThreadPoolExecutor(max_workers=max_workers) as executor:
        # Submit all scan tasks
        futures = {
            executor.submit(scan_resource, resource, rm, config_manager): resource
            for resource in instruments
        }

        # Collect results as they complete
        for future in as_completed(futures):
            resource = futures[future]
            try:
                result = future.result(timeout=10)
                if result:
                    inst_type = result["type"]

                    # Store all devices of each type (support multiple scopes)
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

    # Close the resource manager
    try:
        rm.close()
    except Exception:
        pass

    # Output JSON to stdout (this is the ONLY thing that goes to stdout)
    debug(f"Detection complete. Found: {list(detected_devices.keys())}")
    print(json.dumps(detected_devices))


if __name__ == "__main__":
    get_devices()