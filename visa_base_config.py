import sys
import json
import time
import pyvisa

from visa_checker import run_detection, get_command, debug


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

            # Vertical position -- lets the idle/baseline level sit somewhere
            # other than dead-center (e.g. near the top of the graticule).
            # 0 (default if not set) keeps the old centered behavior.
            channel_offset_cmd = get_command(cfg, "channel_offset", channel=1)
            offset_val = base.get("channel_offset", 0)
            if channel_offset_cmd and offset_val:
                inst.write(f"{channel_offset_cmd} {offset_val}")
                time.sleep(0.05)

            # Acquisition averaging -- smooths out noise across repeated
            # triggers. Only enabled if "acquisition_averages" is present
            # in baseconfig; omitting it keeps the scope's normal (single-
            # shot) acquisition mode untouched.
            averages_val = base.get("acquisition_averages")
            if averages_val:
                acq_type_cmd = get_command(cfg, "acquisition_type")
                if acq_type_cmd:
                    inst.write(f"{acq_type_cmd} AVERages")
                    time.sleep(0.05)

                acq_averages_cmd = get_command(cfg, "acquisition_averages")
                if acq_averages_cmd:
                    inst.write(f"{acq_averages_cmd} {averages_val}")
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


def apply_to_devices(detected_devices):
    """
    Given a devices dict shaped like visa_checker's output
    (e.g. {"oscilloscope": [{"resource": ..., "config": ...}, ...], ...}),
    push base config to every oscilloscope entry. Generators are skipped
    on purpose -- they're configured by the test script that uses them.
    """
    applied = []
    for inst_type, devices in detected_devices.items():
        for device in devices:
            cfg = device.get("config")
            if not cfg:
                continue

            if inst_type == "oscilloscope":
                rm = None
                inst = None
                try:
                    debug(f"Applying base config to {inst_type} on {device['resource']}")
                    rm = pyvisa.ResourceManager()
                    inst = rm.open_resource(device["resource"])
                    inst.timeout = 3000
                    ok = apply_base_config(inst, cfg)
                    applied.append({"resource": device["resource"], "success": ok})
                    debug(f"Base config applied to {device['resource']}")
                except Exception as e:
                    debug(f"Failed to apply base config to {device['resource']}: {e}")
                    applied.append({"resource": device["resource"], "success": False, "error": str(e)})
                finally:
                    try:
                        if inst is not None:
                            inst.close()
                    except Exception:
                        pass
                    try:
                        if rm is not None:
                            rm.close()
                    except Exception:
                        pass
            else:
                debug(f"Skipping {inst_type} on {device['resource']} - not an oscilloscope")

    return applied


def main():
    force_refresh = "--refresh" in sys.argv

    # If a devices JSON file path is passed, use that instead of re-detecting
    # (useful if the caller already ran visa_checker.py and has the output).
    devices_path = None
    for arg in sys.argv[1:]:
        if arg.endswith(".json"):
            devices_path = arg
            break

    if devices_path:
        # utf-8-sig transparently strips a BOM if present (e.g. files written
        # by PowerShell's `Out-File -Encoding utf8`), and behaves exactly like
        # plain utf-8 when there's no BOM.
        with open(devices_path, "r", encoding="utf-8-sig") as f:
            detected_devices = json.load(f)
    else:
        detected_devices = run_detection(force_refresh)

    results = apply_to_devices(detected_devices)
    print(json.dumps({"applied": results}))


if __name__ == "__main__":
    main()