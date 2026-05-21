using System.Text.Json;
namespace TestGen.Core.Validation.Report;
public sealed class ScenarioReportValidator : IScenarioReportValidator
{
    private static readonly HashSet<string> s_knownRoot = new(StringComparer.Ordinal)
    {
        "scenario_id","name","ecu_id","target_execution","description","author","project","tags",
        "created_at","version","priority","export_formats","profile","initial_conditions",
        "ecu_firmware_version","ecu_mode","ecu_power_state","ecu_temperature_C","hw_interface",
        "diagnostic_session","signal_list","sequence_script","event_triggers","bus_config","faults",
        "environmental_perturbations","timing","safety","assertions","sweep_parameters",
        "combinatorial_strategy","max_generated_scenarios","selection_strategy","search_algorithm",
        "reporting","execution","advanced","ecu_catalog_entry","security",
        "unit_test","integration_test","stress_test"
    };
    public ScenarioValidationReport Validate(JsonElement root)
    {
        var c = new List<ValidationCheck>();
        RunMeta(root, c);
        RunProfile(root, c);
        RunInitialConditions(root, c);
        RunEcu(root, c);
        RunSignals(root, c);
        RunBus(root, c);
        RunFaults(root, c);
        RunTiming(root, c);
        RunSafety(root, c);
        RunSweep(root, c);
        RunReporting(root, c);
        RunExecution(root, c);
        RunAdvanced(root, c);
        RunCatalog(root, c);
        RunSchema(root, c);
        RunSecurity(root, c);
        RunTestTypes(root, c);
        RunCrossFields(root, c);
        return new ScenarioValidationReport
        {
            Summary = new ValidationSummary
            {
                TotalChecks = c.Count,
                Passed   = c.Count(x => x.Status == "PASS"),
                Failed   = c.Count(x => x.Status == "FAIL"),
                Warnings = c.Count(x => x.Status == "WARN"),
                Info     = c.Count(x => x.Status == "INFO")
            },
            Checks = c
        };
    }
    private static bool Nav(JsonElement el, string path, out JsonElement r)
    {
        r = el;
        foreach (var s in path.Split('.'))
            if (r.ValueKind != JsonValueKind.Object || !r.TryGetProperty(s, out r)) return false;
        return true;
    }
    private static bool Str(JsonElement root, string path, out string v)
    {
        v = string.Empty;
        if (!Nav(root, path, out var e) || e.ValueKind != JsonValueKind.String) return false;
        v = e.GetString() ?? string.Empty;
        return true;
    }
    private static bool Num(JsonElement root, string path, out double v)
    {
        v = 0;
        if (!Nav(root, path, out var e) || e.ValueKind != JsonValueKind.Number) return false;
        return e.TryGetDouble(out v);
    }
    private static bool Arr(JsonElement root, string path, out JsonElement a)
    {
        a = default;
        if (!Nav(root, path, out var e) || e.ValueKind != JsonValueKind.Array) return false;
        a = e; return true;
    }
    private static bool Obj(JsonElement root, string path, out JsonElement o)
    {
        o = default;
        if (!Nav(root, path, out var e) || e.ValueKind != JsonValueKind.Object) return false;
        o = e; return true;
    }
    private static bool Has(JsonElement root, string path) => Nav(root, path, out _);
    private static bool IsUuid(string s)
        => Guid.TryParseExact(s, "D", out _) || Guid.TryParseExact(s, "N", out _)
        || Guid.TryParseExact(s, "B", out _) || Guid.TryParseExact(s, "P", out _);
    private static bool IsIso8601(string s)
        => DateTimeOffset.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
               System.Globalization.DateTimeStyles.RoundtripKind, out _);
    private static bool IsUri(string s) => Uri.TryCreate(s, UriKind.Absolute, out _);
    private static ValidationCheck MkCheck(string id, string cat, string field, string status, string msg)
        => new() { Id = id, Category = cat, Field = field, Status = status, Message = msg };
    private static ValidationCheck Pass(string id, string c, string f, string m) => MkCheck(id, c, f, "PASS", m);
    private static ValidationCheck Fail(string id, string c, string f, string m) => MkCheck(id, c, f, "FAIL", m);
    private static ValidationCheck Warn(string id, string c, string f, string m) => MkCheck(id, c, f, "WARN", m);
    private static ValidationCheck Info(string id, string c, string f, string m) => MkCheck(id, c, f, "INFO", m);
    private static void CheckNumRange(JsonElement root, string id, string cat, string path, double min, double max, List<ValidationCheck> c)
    {
        if (Num(root, path, out var v))
            c.Add(v >= min && v <= max
                ? Pass(id, cat, path, $"{path} = {v} is within [{min}..{max}].")
                : Fail(id, cat, path, $"{path} = {v} is out of allowed range [{min}..{max}]."));
        else if (!Has(root, path))
            c.Add(Info(id, cat, path, $"Optional field '{path}' not provided."));
        else
            c.Add(Fail(id, cat, path, $"'{path}' must be a number."));
    }
    private static void CheckEnum(JsonElement root, string id, string cat, string path, string[] vals, List<ValidationCheck> c, bool optional = true)
    {
        if (Str(root, path, out var v))
        {
            var set = new HashSet<string>(vals, StringComparer.Ordinal);
            c.Add(set.Contains(v)
                ? Pass(id, cat, path, $"'{path}' = '{v}' is valid.")
                : Fail(id, cat, path, $"'{path}' = '{v}' must be one of: {string.Join(", ", vals)}."));
        }
        else if (!Has(root, path))
            c.Add(optional
                ? Info(id, cat, path, $"Optional field '{path}' not provided.")
                : Fail(id, cat, path, $"Required field '{path}' is missing."));
        else
            c.Add(Fail(id, cat, path, $"'{path}' must be a string."));
    }
    private static void RunMeta(JsonElement root, List<ValidationCheck> c)
    {
        const string cat = "Metadata";
        if (Str(root, "scenario_id", out var sid))
            c.Add(IsUuid(sid) ? Pass("META-01", cat, "scenario_id", $"'{sid}' is a valid UUID.")
                              : Fail("META-01", cat, "scenario_id", $"'{sid}' is not a valid UUID."));
        else
            c.Add(Fail("META-01", cat, "scenario_id", "Required field 'scenario_id' is missing or not a string."));
        if (Str(root, "name", out var name))
            c.Add(name.Length > 0 ? Pass("META-02", cat, "name", "name is present and non-empty.")
                                  : Fail("META-02", cat, "name", "name is an empty string."));
        else
            c.Add(Fail("META-02", cat, "name", "Required field 'name' is missing."));
        if (Str(root, "ecu_id", out var ecuId))
            c.Add(ecuId.Length > 0 ? Pass("META-03", cat, "ecu_id", "ecu_id is present and non-empty.")
                                   : Fail("META-03", cat, "ecu_id", "ecu_id is an empty string."));
        else
            c.Add(Fail("META-03", cat, "ecu_id", "Required field 'ecu_id' is missing."));
        CheckEnum(root, "META-04", cat, "target_execution",
            new[] { "simulation", "SIL", "HIL", "bench", "lab" }, c, optional: false);
        c.Add(Has(root, "description")
            ? Pass("META-05", cat, "description", "description is present.")
            : Info("META-05", cat, "description", "Optional field 'description' not provided."));
        c.Add(Has(root, "author")
            ? Pass("META-06", cat, "author", "author is present.")
            : Info("META-06", cat, "author", "Optional field 'author' not provided."));
        c.Add(Has(root, "project")
            ? Pass("META-07", cat, "project", "project is present.")
            : Info("META-07", cat, "project", "Optional field 'project' not provided."));
        if (Arr(root, "tags", out var tags))
        {
            var bad = tags.EnumerateArray().Where(t => t.ValueKind != JsonValueKind.String).ToList();
            c.Add(bad.Count == 0
                ? Pass("META-08", cat, "tags", $"tags contains {tags.GetArrayLength()} valid string(s).")
                : Fail("META-08", cat, "tags", "tags array contains non-string items."));
        }
        else if (!Has(root, "tags"))
            c.Add(Info("META-08", cat, "tags", "Optional field 'tags' not provided."));
        else
            c.Add(Fail("META-08", cat, "tags", "tags must be an array of strings."));
        if (Str(root, "created_at", out var ca))
            c.Add(IsIso8601(ca) ? Pass("META-09", cat, "created_at", "created_at is a valid ISO 8601 date-time.")
                                : Fail("META-09", cat, "created_at", $"'{ca}' is not a valid ISO 8601 date-time."));
        else if (!Has(root, "created_at"))
            c.Add(Info("META-09", cat, "created_at", "Optional field 'created_at' not provided."));
        else
            c.Add(Fail("META-09", cat, "created_at", "created_at must be a string."));
        c.Add(Has(root, "version")
            ? Pass("META-10", cat, "version", "version is present.")
            : Info("META-10", cat, "version", "Optional field 'version' not provided."));
        CheckEnum(root, "META-11", cat, "priority", new[] { "low", "medium", "high", "critical" }, c);
        var fmtVals = new HashSet<string> { "JSON", "OpenSCENARIO", "XCP", "CANoe", "Vector", "CSV", "TestScript" };
        if (Arr(root, "export_formats", out var fmts))
        {
            var bad = fmts.EnumerateArray()
                .Where(f => f.ValueKind != JsonValueKind.String || !fmtVals.Contains(f.GetString()!))
                .Select(f => f.ToString()).ToList();
            c.Add(bad.Count == 0
                ? Pass("META-12", cat, "export_formats", "All export_formats values are valid.")
                : Fail("META-12", cat, "export_formats", $"Invalid format(s): {string.Join(", ", bad)}. Must be one of: {string.Join(", ", fmtVals)}."));
        }
        else if (!Has(root, "export_formats"))
            c.Add(Info("META-12", cat, "export_formats", "Optional field 'export_formats' not provided."));
        else
            c.Add(Fail("META-12", cat, "export_formats", "export_formats must be an array."));
    }
    private static void RunProfile(JsonElement root, List<ValidationCheck> c)
    {
        const string cat = "Profile";
        if (!Obj(root, "profile", out var prof))
        {
            c.Add(Info("PROF-01", cat, "profile", "Optional block 'profile' not provided. PROF-01 to PROF-06 skipped."));
            return;
        }
        c.Add(Has(prof, "profile_id") && prof.TryGetProperty("profile_id", out var pid) && pid.ValueKind == JsonValueKind.String
            ? Pass("PROF-01", cat, "profile.profile_id", "profile_id is present.")
            : Fail("PROF-01", cat, "profile.profile_id", "profile.profile_id is required when profile block is present."));
        c.Add(Has(prof, "name") && prof.TryGetProperty("name", out var pn) && pn.ValueKind == JsonValueKind.String
            ? Pass("PROF-02", cat, "profile.name", "profile.name is present.")
            : Fail("PROF-02", cat, "profile.name", "profile.name is required when profile block is present."));
        c.Add(Has(prof, "description")
            ? Pass("PROF-03", cat, "profile.description", "profile.description is present.")
            : Info("PROF-03", cat, "profile.description", "Optional field 'profile.description' not provided."));
        if (Has(prof, "default_parameters"))
            c.Add(prof.TryGetProperty("default_parameters", out var dp) && dp.ValueKind == JsonValueKind.Object
                ? Pass("PROF-04", cat, "profile.default_parameters", "profile.default_parameters is a valid object.")
                : Fail("PROF-04", cat, "profile.default_parameters", "profile.default_parameters must be an object."));
        else
            c.Add(Info("PROF-04", cat, "profile.default_parameters", "Optional field 'profile.default_parameters' not provided."));
        if (Arr(prof, "locked_parameters", out var lp))
            c.Add(Pass("PROF-05", cat, "profile.locked_parameters", $"locked_parameters has {lp.GetArrayLength()} entry/entries."));
        else if (!Has(prof, "locked_parameters"))
            c.Add(Info("PROF-05", cat, "profile.locked_parameters", "Optional field 'profile.locked_parameters' not provided."));
        else
            c.Add(Fail("PROF-05", cat, "profile.locked_parameters", "locked_parameters must be an array."));
        if (Arr(prof, "recommended_assertions", out var ra))
            c.Add(Pass("PROF-06", cat, "profile.recommended_assertions", $"recommended_assertions has {ra.GetArrayLength()} entry/entries."));
        else if (!Has(prof, "recommended_assertions"))
            c.Add(Info("PROF-06", cat, "profile.recommended_assertions", "Optional field 'profile.recommended_assertions' not provided."));
        else
            c.Add(Fail("PROF-06", cat, "profile.recommended_assertions", "recommended_assertions must be an array."));
    }
    private static void RunInitialConditions(JsonElement root, List<ValidationCheck> c)
    {
        const string cat = "Initial Conditions";
        if (!Has(root, "initial_conditions"))
        {
            c.Add(Info("IC-01", cat, "initial_conditions", "Optional block 'initial_conditions' not provided. IC-01 to IC-10 skipped."));
            return;
        }
        CheckNumRange(root, "IC-01", cat, "initial_conditions.ambient_temperature_C", -40, 85, c);
        CheckNumRange(root, "IC-02", cat, "initial_conditions.battery_voltage_V", 6.0, 16.0, c);
        CheckEnum(root, "IC-03", cat, "initial_conditions.engine_state", new[] { "OFF", "IDLE", "RUNNING" }, c);
        CheckNumRange(root, "IC-04", cat, "initial_conditions.fuel_level_percent", 0, 100, c);
        CheckNumRange(root, "IC-05", cat, "initial_conditions.vehicle_speed_mps", 0, 100, c);
        CheckEnum(root, "IC-06", cat, "initial_conditions.gear_position", new[] { "P", "R", "N", "D", "1", "2", "3" }, c);
        CheckEnum(root, "IC-07", cat, "initial_conditions.ignition_key", new[] { "OFF", "ON" }, c);
        CheckNumRange(root, "IC-08", cat, "initial_conditions.ambient_humidity_pct", 0, 100, c);
        CheckEnum(root, "IC-09", cat, "initial_conditions.gps_fix_quality", new[] { "NONE", "POOR", "GOOD", "EXCELLENT" }, c);
        CheckNumRange(root, "IC-10", cat, "initial_conditions.localization_error_m", 0, 100, c);
    }
    private static void RunEcu(JsonElement root, List<ValidationCheck> c)
    {
        const string cat = "ECU Config";
        if (Has(root, "ecu_firmware_version"))
            c.Add(Str(root, "ecu_firmware_version", out _)
                ? Pass("ECU-01", cat, "ecu_firmware_version", "ecu_firmware_version is present.")
                : Fail("ECU-01", cat, "ecu_firmware_version", "ecu_firmware_version must be a string."));
        else
            c.Add(Info("ECU-01", cat, "ecu_firmware_version", "Optional field 'ecu_firmware_version' not provided."));
        CheckEnum(root, "ECU-02", cat, "ecu_mode", new[] { "NORMAL", "DIAGNOSTIC", "SAFE", "CALIBRATION" }, c);
        CheckEnum(root, "ECU-03", cat, "ecu_power_state", new[] { "ON", "OFF", "SLEEP" }, c);
        CheckNumRange(root, "ECU-04", cat, "ecu_temperature_C", -40, 125, c);
        var validIface = new HashSet<string> { "CAN", "CAN-FD", "LIN", "FlexRay", "Ethernet", "UART", "SPI", "I2C" };
        if (Arr(root, "hw_interface", out var hw))
        {
            var bad = hw.EnumerateArray()
                .Where(i => i.ValueKind != JsonValueKind.String || !validIface.Contains(i.GetString()!))
                .Select(i => i.ToString()).ToList();
            c.Add(bad.Count == 0
                ? Pass("ECU-05", cat, "hw_interface", $"All {hw.GetArrayLength()} hw_interface value(s) are valid.")
                : Fail("ECU-05", cat, "hw_interface", $"Invalid hw_interface value(s): {string.Join(", ", bad)}."));
        }
        else if (!Has(root, "hw_interface"))
            c.Add(Info("ECU-05", cat, "hw_interface", "Optional field 'hw_interface' not provided."));
        else
            c.Add(Fail("ECU-05", cat, "hw_interface", "hw_interface must be an array."));
        CheckEnum(root, "ECU-06", cat, "diagnostic_session", new[] { "NONE", "DEFAULT", "EXTENDED" }, c);
    }
    private static void RunSignals(JsonElement root, List<ValidationCheck> c)
    {
        const string cat = "Signals";
        var validBeh = new HashSet<string> { "static", "step", "ramp", "sine", "noise", "random_walk", "scripted" };
        if (Arr(root, "signal_list", out var sigs))
        {
            int idx = 0;
            bool allSig01 = true;
            foreach (var sig in sigs.EnumerateArray())
            {
                bool hasName = sig.TryGetProperty("name", out var nProp) && nProp.ValueKind == JsonValueKind.String;
                bool hasType = sig.TryGetProperty("type", out var tProp) && tProp.ValueKind == JsonValueKind.String
                               && (tProp.GetString() == "analog" || tProp.GetString() == "digital");
                if (!hasName || !hasType) allSig01 = false;
                string sigLabel = hasName ? nProp.GetString()! : idx.ToString();
                if (sig.TryGetProperty("min", out var minEl) && sig.TryGetProperty("max", out var maxEl)
                    && minEl.ValueKind == JsonValueKind.Number && maxEl.ValueKind == JsonValueKind.Number)
                    c.Add(minEl.GetDouble() <= maxEl.GetDouble()
                        ? Pass("SIG-04", cat, $"signal_list[{idx}].min/max", $"Signal '{sigLabel}': min <= max.")
                        : Fail("SIG-04", cat, $"signal_list[{idx}].min/max", $"Signal '{sigLabel}': min ({minEl.GetDouble()}) > max ({maxEl.GetDouble()})."));
                if (sig.TryGetProperty("behavior", out var beh))
                {
                    bool validB = beh.ValueKind == JsonValueKind.String && validBeh.Contains(beh.GetString()!);
                    c.Add(validB
                        ? Pass("SIG-06", cat, $"signal_list[{idx}].behavior", $"behavior '{beh.GetString()}' is valid.")
                        : Fail("SIG-06", cat, $"signal_list[{idx}].behavior", $"behavior '{beh}' must be one of: {string.Join(", ", validBeh)}."));
                    if (beh.ValueKind == JsonValueKind.String)
                    {
                        var bStr = beh.GetString();
                        if (bStr == "step")
                        {
                            if (!sig.TryGetProperty("step_time_ms", out var stms))
                                c.Add(Warn("SIG-09", cat, $"signal_list[{idx}].step_time_ms", $"Signal '{sigLabel}': behavior 'step' but 'step_time_ms' not provided."));
                            else if (stms.ValueKind == JsonValueKind.Number && stms.GetDouble() < 0)
                                c.Add(Fail("SIG-09", cat, $"signal_list[{idx}].step_time_ms", $"Signal '{sigLabel}': step_time_ms must be >= 0."));
                            else
                                c.Add(Pass("SIG-09", cat, $"signal_list[{idx}].step_time_ms", $"Signal '{sigLabel}': step_time_ms is valid."));
                        }
                        else if (bStr == "ramp")
                        {
                            bool hasRise = sig.TryGetProperty("rise_time_ms", out var rms);
                            bool hasFall = sig.TryGetProperty("fall_time_ms", out var fms);
                            if (!hasRise && !hasFall)
                                c.Add(Warn("SIG-09", cat, $"signal_list[{idx}].rise/fall_time_ms", $"Signal '{sigLabel}': behavior 'ramp' but neither 'rise_time_ms' nor 'fall_time_ms' provided."));
                            else
                            {
                                if (hasRise && rms.ValueKind == JsonValueKind.Number && rms.GetDouble() < 0)
                                    c.Add(Fail("SIG-09", cat, $"signal_list[{idx}].rise_time_ms", $"Signal '{sigLabel}': rise_time_ms must be >= 0."));
                                if (hasFall && fms.ValueKind == JsonValueKind.Number && fms.GetDouble() < 0)
                                    c.Add(Fail("SIG-09", cat, $"signal_list[{idx}].fall_time_ms", $"Signal '{sigLabel}': fall_time_ms must be >= 0."));
                            }
                        }
                    }
                }
                idx++;
            }
            c.Add(allSig01
                ? Pass("SIG-01", cat, "signal_list", $"All {sigs.GetArrayLength()} signal(s) have valid 'name' and 'type'.")
                : Fail("SIG-01", cat, "signal_list", "One or more signals are missing 'name' (string) or 'type' (analog/digital)."));
        }
        else if (!Has(root, "signal_list"))
            c.Add(Info("SIG-01", cat, "signal_list", "Optional block 'signal_list' not provided. Signal-level checks skipped."));
        else
            c.Add(Fail("SIG-01", cat, "signal_list", "signal_list must be an array."));
        if (Arr(root, "sequence_script", out var seq))
        {
            bool allOk = true;
            int si = 0;
            foreach (var step in seq.EnumerateArray())
            {
                bool okTime = step.TryGetProperty("time_ms", out var tms) && tms.ValueKind == JsonValueKind.Number && tms.GetDouble() >= 0;
                bool okAct  = step.TryGetProperty("action", out var act) && act.ValueKind == JsonValueKind.String;
                if (!okTime || !okAct)
                {
                    allOk = false;
                    c.Add(Fail("SIG-07", cat, $"sequence_script[{si}]", $"Step {si}: missing or invalid 'time_ms' (>= 0) or 'action' (string)."));
                }
                si++;
            }
            if (allOk) c.Add(Pass("SIG-07", cat, "sequence_script", $"All {seq.GetArrayLength()} sequence_script step(s) are valid."));
        }
        else if (!Has(root, "sequence_script"))
            c.Add(Info("SIG-07", cat, "sequence_script", "Optional field 'sequence_script' not provided."));
        else
            c.Add(Fail("SIG-07", cat, "sequence_script", "sequence_script must be an array."));
        Num(root, "timing.test_duration_ms", out var testDurSig);
        if (Arr(root, "event_triggers", out var evts))
        {
            bool allOk = true;
            int ei = 0;
            foreach (var evt in evts.EnumerateArray())
            {
                bool hasTrigger = evt.TryGetProperty("time_ms", out _) || evt.TryGetProperty("condition", out _);
                bool hasAct = evt.TryGetProperty("action", out var ea) && ea.ValueKind == JsonValueKind.String;
                if (!hasTrigger || !hasAct)
                {
                    allOk = false;
                    c.Add(Fail("SIG-08", cat, $"event_triggers[{ei}]", $"Trigger {ei}: must have at least 'time_ms' or 'condition', plus 'action' (string)."));
                }
                else if (evt.TryGetProperty("time_ms", out var ems) && ems.ValueKind == JsonValueKind.Number
                         && testDurSig > 0 && ems.GetDouble() > testDurSig)
                    c.Add(Warn("SIG-08", cat, $"event_triggers[{ei}].time_ms", $"Trigger {ei}: time_ms ({ems.GetDouble()}) exceeds test_duration_ms ({testDurSig})."));
                ei++;
            }
            if (allOk) c.Add(Pass("SIG-08", cat, "event_triggers", $"All {evts.GetArrayLength()} event_trigger(s) are valid."));
        }
        else if (!Has(root, "event_triggers"))
            c.Add(Info("SIG-08", cat, "event_triggers", "Optional field 'event_triggers' not provided."));
        else
            c.Add(Fail("SIG-08", cat, "event_triggers", "event_triggers must be an array."));
    }
    private static void RunBus(JsonElement root, List<ValidationCheck> c)
    {
        const string cat = "Bus Config";
        if (!Obj(root, "bus_config", out var bus))
        {
            c.Add(Info("BUS-01", cat, "bus_config", "Optional block 'bus_config' not provided. BUS-01 to BUS-06 skipped."));
            return;
        }
        c.Add(Has(bus, "bus_name")
            ? Pass("BUS-01", cat, "bus_config.bus_name", "bus_name is present.")
            : Info("BUS-01", cat, "bus_config.bus_name", "Optional field 'bus_name' not provided."));
        CheckEnum(bus, "BUS-02", cat, "bus_type", new[] { "CAN", "CAN-FD", "LIN", "FlexRay", "Ethernet" }, c);
        if (Num(bus, "bitrate_kbps", out var br))
            c.Add(br >= 1 ? Pass("BUS-03", cat, "bus_config.bitrate_kbps", $"bitrate_kbps = {br} is valid.")
                          : Fail("BUS-03", cat, "bus_config.bitrate_kbps", $"bitrate_kbps = {br} must be >= 1."));
        else if (!Has(bus, "bitrate_kbps"))
            c.Add(Info("BUS-03", cat, "bus_config.bitrate_kbps", "Optional field 'bitrate_kbps' not provided."));
        else
            c.Add(Fail("BUS-03", cat, "bus_config.bitrate_kbps", "bitrate_kbps must be a number."));
        if (Arr(bus, "message_defs", out var mdefs))
        {
            bool allOk = true;
            int mi = 0;
            foreach (var msg in mdefs.EnumerateArray())
            {
                bool hasId   = msg.TryGetProperty("id", out _);
                bool hasName = msg.TryGetProperty("name", out var mn) && mn.ValueKind == JsonValueKind.String;
                if (!hasId || !hasName)
                {
                    allOk = false;
                    c.Add(Fail("BUS-04", cat, $"bus_config.message_defs[{mi}]", $"Message def {mi}: missing required 'id' or 'name'."));
                }
                if (msg.TryGetProperty("period_ms", out var pms) && pms.ValueKind == JsonValueKind.Number && pms.GetDouble() < 0)
                    c.Add(Fail("BUS-04", cat, $"bus_config.message_defs[{mi}].period_ms", $"Message def {mi}: period_ms must be >= 0."));
                mi++;
            }
            if (allOk) c.Add(Pass("BUS-04", cat, "bus_config.message_defs", $"All {mdefs.GetArrayLength()} message def(s) are valid."));
        }
        else if (!Has(bus, "message_defs"))
            c.Add(Info("BUS-04", cat, "bus_config.message_defs", "Optional field 'message_defs' not provided."));
        else
            c.Add(Fail("BUS-04", cat, "bus_config.message_defs", "message_defs must be an array."));
        if (Num(bus, "bus_load_pct", out var blp))
            c.Add(blp >= 0 && blp <= 100
                ? Pass("BUS-05", cat, "bus_config.bus_load_pct", $"bus_load_pct = {blp}% is valid.")
                : Fail("BUS-05", cat, "bus_config.bus_load_pct", $"bus_load_pct = {blp} is out of range [0..100]."));
        else if (!Has(bus, "bus_load_pct"))
            c.Add(Info("BUS-05", cat, "bus_config.bus_load_pct", "Optional field 'bus_load_pct' not provided."));
        else
            c.Add(Fail("BUS-05", cat, "bus_config.bus_load_pct", "bus_load_pct must be a number."));
        if (Num(bus, "message_jitter_ms", out var jit))
            c.Add(jit >= 0 ? Pass("BUS-06", cat, "bus_config.message_jitter_ms", $"message_jitter_ms = {jit} is valid.")
                           : Fail("BUS-06", cat, "bus_config.message_jitter_ms", "message_jitter_ms must be >= 0."));
        else if (!Has(bus, "message_jitter_ms"))
            c.Add(Info("BUS-06", cat, "bus_config.message_jitter_ms", "Optional field 'message_jitter_ms' not provided."));
        else
            c.Add(Fail("BUS-06", cat, "bus_config.message_jitter_ms", "message_jitter_ms must be a number."));
    }
    private static void RunFaults(JsonElement root, List<ValidationCheck> c)
    {
        const string cat = "Fault Injection";
        var validTypes = new HashSet<string> {
            "message_loss","tx_error","rx_error","bit_flip","bus_noise","short_circuit",
            "sensor_spoof","sensor_dropout","actuator_stuck","soft_reboot",
            "high_latency","packet_delay","packet_duplication"
        };
        var validPatterns = new HashSet<string> { "continuous", "burst", "periodic", "random" };
        if (Arr(root, "faults", out var faults))
        {
            int fi = 0;
            foreach (var fault in faults.EnumerateArray())
            {
                var issues = new List<string>();
                if (!fault.TryGetProperty("fault_id", out _)) issues.Add("'fault_id' missing");
                if (!fault.TryGetProperty("type", out var ft))
                    issues.Add("'type' missing");
                else if (ft.ValueKind != JsonValueKind.String || !validTypes.Contains(ft.GetString()!))
                    issues.Add($"'type' '{ft}' is not a valid fault type");
                if (!fault.TryGetProperty("target", out _)) issues.Add("'target' missing");
                if (!fault.TryGetProperty("start_time_ms", out var stms))
                    issues.Add("'start_time_ms' missing");
                else if (stms.ValueKind != JsonValueKind.Number || stms.GetDouble() < 0)
                    issues.Add("'start_time_ms' must be a number >= 0");
                if (!fault.TryGetProperty("duration_ms", out var dms))
                    issues.Add("'duration_ms' missing");
                else if (dms.ValueKind != JsonValueKind.Number || dms.GetDouble() < 0)
                    issues.Add("'duration_ms' must be a number >= 0");
                c.Add(issues.Count == 0
                    ? Pass("FLT-01", cat, $"faults[{fi}]", $"Fault {fi}: all required fields are present and valid.")
                    : Fail("FLT-01", cat, $"faults[{fi}]", $"Fault {fi}: {string.Join("; ", issues)}."));
                if (fault.TryGetProperty("type", out var ftype))
                    c.Add(ftype.ValueKind == JsonValueKind.String && validTypes.Contains(ftype.GetString()!)
                        ? Pass("FLT-02", cat, $"faults[{fi}].type", $"type '{ftype.GetString()}' is valid.")
                        : Fail("FLT-02", cat, $"faults[{fi}].type", $"type '{ftype}' is not a valid fault type."));
                if (fault.TryGetProperty("start_time_ms", out var sfms))
                    c.Add(sfms.ValueKind == JsonValueKind.Number && sfms.GetDouble() >= 0
                        ? Pass("FLT-04", cat, $"faults[{fi}].start_time_ms", $"start_time_ms = {sfms.GetDouble()} is valid.")
                        : Fail("FLT-04", cat, $"faults[{fi}].start_time_ms", "start_time_ms must be a number >= 0."));
                if (fault.TryGetProperty("duration_ms", out var dfms))
                    c.Add(dfms.ValueKind == JsonValueKind.Number && dfms.GetDouble() >= 0
                        ? Pass("FLT-05", cat, $"faults[{fi}].duration_ms", $"duration_ms = {dfms.GetDouble()} is valid.")
                        : Fail("FLT-05", cat, $"faults[{fi}].duration_ms", "duration_ms must be a number >= 0."));
                if (fault.TryGetProperty("intensity", out var inten))
                {
                    if (inten.ValueKind == JsonValueKind.Number)
                    {
                        var iv = inten.GetDouble();
                        c.Add(iv >= 0 && iv <= 1
                            ? Pass("FLT-06", cat, $"faults[{fi}].intensity", $"intensity = {iv} is in [0..1].")
                            : Fail("FLT-06", cat, $"faults[{fi}].intensity", $"intensity = {iv} is out of range [0..1]."));
                    }
                    else
                        c.Add(Fail("FLT-06", cat, $"faults[{fi}].intensity", "intensity must be a number."));
                }
                if (fault.TryGetProperty("pattern", out var pat))
                    c.Add(pat.ValueKind == JsonValueKind.String && validPatterns.Contains(pat.GetString()!)
                        ? Pass("FLT-07", cat, $"faults[{fi}].pattern", $"pattern '{pat.GetString()}' is valid.")
                        : Fail("FLT-07", cat, $"faults[{fi}].pattern", $"pattern '{pat}' must be one of: continuous, burst, periodic, random."));
                if (fault.TryGetProperty("repeat", out var rep))
                    c.Add(rep.ValueKind == JsonValueKind.Number && rep.TryGetInt32(out var ri) && ri >= 0
                        ? Pass("FLT-08", cat, $"faults[{fi}].repeat", $"repeat = {ri} is valid.")
                        : Fail("FLT-08", cat, $"faults[{fi}].repeat", "repeat must be an integer >= 0."));
                fi++;
            }
        }
        else if (!Has(root, "faults"))
            c.Add(Info("FLT-01", cat, "faults", "Optional block 'faults' not provided. FLT-01 to FLT-08 skipped."));
        else
            c.Add(Fail("FLT-01", cat, "faults", "faults must be an array."));
        if (Has(root, "environmental_perturbations"))
        {
            CheckEnum(root, "FLT-09", cat, "environmental_perturbations.EMI_level", new[] { "none", "low", "medium", "high" }, c);
            if (Has(root, "environmental_perturbations.temperature_ramp"))
            {
                bool okS = Num(root, "environmental_perturbations.temperature_ramp.start_C", out _);
                bool okE = Num(root, "environmental_perturbations.temperature_ramp.end_C", out _);
                bool okR = Num(root, "environmental_perturbations.temperature_ramp.rate_C_per_min", out _);
                c.Add(okS && okE && okR
                    ? Pass("FLT-10", cat, "environmental_perturbations.temperature_ramp", "temperature_ramp has valid start_C, end_C, rate_C_per_min.")
                    : Fail("FLT-10", cat, "environmental_perturbations.temperature_ramp", "temperature_ramp must have 'start_C', 'end_C', 'rate_C_per_min' (numbers)."));
            }
            else
                c.Add(Info("FLT-10", cat, "environmental_perturbations.temperature_ramp", "Optional field 'temperature_ramp' not provided."));
        }
        else
            c.Add(Info("FLT-09", cat, "environmental_perturbations", "Optional block 'environmental_perturbations' not provided. FLT-09 and FLT-10 skipped."));
    }
    private static void RunTiming(JsonElement root, List<ValidationCheck> c)
    {
        const string cat = "Timing";
        if (!Has(root, "timing"))
        {
            c.Add(Info("TIM-01", cat, "timing", "Optional block 'timing' not provided. TIM-01 to TIM-04 skipped."));
            return;
        }
        if (Num(root, "timing.test_duration_ms", out var td))
            c.Add(td >= 0 ? Pass("TIM-01", cat, "timing.test_duration_ms", $"test_duration_ms = {td} ms.")
                          : Fail("TIM-01", cat, "timing.test_duration_ms", "test_duration_ms must be >= 0."));
        else if (!Has(root, "timing.test_duration_ms"))
            c.Add(Info("TIM-01", cat, "timing.test_duration_ms", "Optional field 'test_duration_ms' not provided."));
        else
            c.Add(Fail("TIM-01", cat, "timing.test_duration_ms", "test_duration_ms must be a number."));
        if (Num(root, "timing.time_resolution_ms", out var tr))
            c.Add(tr >= 1 ? Pass("TIM-02", cat, "timing.time_resolution_ms", $"time_resolution_ms = {tr} ms.")
                          : Fail("TIM-02", cat, "timing.time_resolution_ms", $"time_resolution_ms = {tr} must be >= 1."));
        else if (!Has(root, "timing.time_resolution_ms"))
            c.Add(Info("TIM-02", cat, "timing.time_resolution_ms", "Optional field 'time_resolution_ms' not provided."));
        else
            c.Add(Fail("TIM-02", cat, "timing.time_resolution_ms", "time_resolution_ms must be a number."));
        if (Nav(root, "timing.repetitions", out var repEl))
            c.Add(repEl.ValueKind == JsonValueKind.Number && repEl.TryGetInt32(out var rep) && rep >= 1
                ? Pass("TIM-03", cat, "timing.repetitions", $"repetitions = {rep}.")
                : Fail("TIM-03", cat, "timing.repetitions", "repetitions must be an integer >= 1."));
        else
            c.Add(Info("TIM-03", cat, "timing.repetitions", "Optional field 'repetitions' not provided."));
        if (Num(root, "timing.inter_test_delay_ms", out var itd))
            c.Add(itd >= 0 ? Pass("TIM-04", cat, "timing.inter_test_delay_ms", $"inter_test_delay_ms = {itd} ms.")
                           : Fail("TIM-04", cat, "timing.inter_test_delay_ms", "inter_test_delay_ms must be >= 0."));
        else if (!Has(root, "timing.inter_test_delay_ms"))
            c.Add(Info("TIM-04", cat, "timing.inter_test_delay_ms", "Optional field 'inter_test_delay_ms' not provided."));
        else
            c.Add(Fail("TIM-04", cat, "timing.inter_test_delay_ms", "inter_test_delay_ms must be a number."));
    }
    private static void RunSafety(JsonElement root, List<ValidationCheck> c)
    {
        const string cat = "Safety";
        if (!Has(root, "safety"))
            c.Add(Info("SAF-01", cat, "safety", "Optional block 'safety' not provided. SAF-01 to SAF-05 skipped."));
        else
        {
            c.Add(Has(root, "safety.hazard_id")
                ? Pass("SAF-01", cat, "safety.hazard_id", "hazard_id is present.")
                : Info("SAF-01", cat, "safety.hazard_id", "Optional field 'safety.hazard_id' not provided."));
            c.Add(Has(root, "safety.safety_goal")
                ? Pass("SAF-02", cat, "safety.safety_goal", "safety_goal is present.")
                : Info("SAF-02", cat, "safety.safety_goal", "Optional field 'safety.safety_goal' not provided."));
            CheckEnum(root, "SAF-03", cat, "safety.ASIL_level", new[] { "QM", "A", "B", "C", "D" }, c);
            if (Num(root, "safety.risk_weight", out var rw))
                c.Add(rw >= 0 && rw <= 1
                    ? Pass("SAF-04", cat, "safety.risk_weight", $"risk_weight = {rw} is in [0..1].")
                    : Fail("SAF-04", cat, "safety.risk_weight", $"risk_weight = {rw} is out of range [0..1]."));
            else if (!Has(root, "safety.risk_weight"))
                c.Add(Info("SAF-04", cat, "safety.risk_weight", "Optional field 'safety.risk_weight' not provided."));
            else
                c.Add(Fail("SAF-04", cat, "safety.risk_weight", "safety.risk_weight must be a number."));
            if (Has(root, "safety.recovery_behaviour_expected"))
            {
                Nav(root, "safety.recovery_behaviour_expected", out var rbv);
                c.Add(rbv.ValueKind is JsonValueKind.True or JsonValueKind.False or JsonValueKind.Object
                    ? Pass("SAF-05", cat, "safety.recovery_behaviour_expected", "recovery_behaviour_expected is a valid boolean or object.")
                    : Fail("SAF-05", cat, "safety.recovery_behaviour_expected", "recovery_behaviour_expected must be a boolean or object."));
            }
            else
                c.Add(Info("SAF-05", cat, "safety.recovery_behaviour_expected", "Optional field 'safety.recovery_behaviour_expected' not provided."));
        }
        var validAsrTypes = new HashSet<string> { "STL", "LTL", "metric", "boolean" };
        var validAsrSev   = new HashSet<string> { "warning", "minor", "major", "critical" };
        if (Arr(root, "assertions", out var asrs))
        {
            int ai = 0;
            foreach (var asr in asrs.EnumerateArray())
            {
                var issues = new List<string>();
                if (!asr.TryGetProperty("id", out var aid) || aid.ValueKind != JsonValueKind.String)
                    issues.Add("'id' missing or not a string");
                if (!asr.TryGetProperty("type", out var at))
                    issues.Add("'type' missing");
                else if (at.ValueKind != JsonValueKind.String || !validAsrTypes.Contains(at.GetString()!))
                    issues.Add($"'type' '{at}' must be one of: STL, LTL, metric, boolean");
                if (!asr.TryGetProperty("expr", out var ae) || ae.ValueKind != JsonValueKind.String)
                    issues.Add("'expr' missing or not a string");
                c.Add(issues.Count == 0
                    ? Pass("ASR-01", cat, $"assertions[{ai}]", $"Assertion {ai}: all required fields present.")
                    : Fail("ASR-01", cat, $"assertions[{ai}]", $"Assertion {ai}: {string.Join("; ", issues)}."));
                if (asr.TryGetProperty("type", out var asrT))
                    c.Add(asrT.ValueKind == JsonValueKind.String && validAsrTypes.Contains(asrT.GetString()!)
                        ? Pass("ASR-02", cat, $"assertions[{ai}].type", $"type '{asrT.GetString()}' is valid.")
                        : Fail("ASR-02", cat, $"assertions[{ai}].type", $"type '{asrT}' must be STL, LTL, metric, or boolean."));
                if (asr.TryGetProperty("severity", out var sev))
                    c.Add(sev.ValueKind == JsonValueKind.String && validAsrSev.Contains(sev.GetString()!)
                        ? Pass("ASR-03", cat, $"assertions[{ai}].severity", $"severity '{sev.GetString()}' is valid.")
                        : Fail("ASR-03", cat, $"assertions[{ai}].severity", $"severity '{sev}' must be: warning, minor, major, critical."));
                if (asr.TryGetProperty("time_window_ms", out var tw))
                {
                    bool okF = tw.TryGetProperty("from", out var frm) && frm.ValueKind == JsonValueKind.Number && frm.GetDouble() >= 0;
                    bool okT = tw.TryGetProperty("to", out var toE)   && toE.ValueKind  == JsonValueKind.Number && toE.GetDouble()  >= 0;
                    c.Add(okF && okT
                        ? Pass("ASR-04", cat, $"assertions[{ai}].time_window_ms", "time_window_ms has valid 'from' and 'to'.")
                        : Fail("ASR-04", cat, $"assertions[{ai}].time_window_ms", "time_window_ms must have 'from' and 'to' as numbers >= 0."));
                }
                ai++;
            }
        }
        else if (!Has(root, "assertions"))
            c.Add(Info("ASR-01", cat, "assertions", "Optional block 'assertions' not provided. ASR-01 to ASR-04 skipped."));
        else
            c.Add(Fail("ASR-01", cat, "assertions", "assertions must be an array."));
    }
    private static void RunSweep(JsonElement root, List<ValidationCheck> c)
    {
        const string cat = "Parameter Sweep";
        var validSweepTypes = new HashSet<string> {
            "fixed","linear_range","geometric_range","enum_list","random_uniform","gaussian","latin_hypercube"
        };
        if (Obj(root, "sweep_parameters", out var sp))
        {
            bool allOk = true;
            foreach (var prop in sp.EnumerateObject())
            {
                if (prop.Value.ValueKind != JsonValueKind.Object || !prop.Value.TryGetProperty("type", out var sweepType))
                {
                    allOk = false;
                    c.Add(Fail("SWP-01", cat, $"sweep_parameters.{prop.Name}", $"Sweep '{prop.Name}': must be an object with a 'type' field."));
                    continue;
                }
                if (sweepType.ValueKind != JsonValueKind.String || !validSweepTypes.Contains(sweepType.GetString()!))
                {
                    allOk = false;
                    c.Add(Fail("SWP-02", cat, $"sweep_parameters.{prop.Name}.type", $"Sweep '{prop.Name}': type '{sweepType}' must be one of: {string.Join(", ", validSweepTypes)}."));
                    continue;
                }
                c.Add(Pass("SWP-02", cat, $"sweep_parameters.{prop.Name}.type", $"Sweep '{prop.Name}': type '{sweepType.GetString()}' is valid."));
                var t = sweepType.GetString()!;
                if (t is "linear_range" or "geometric_range")
                {
                    bool okF = prop.Value.TryGetProperty("from", out _);
                    bool okT = prop.Value.TryGetProperty("to", out _);
                    bool okS = prop.Value.TryGetProperty("step", out _);
                    if (!okF || !okT || !okS)
                    {
                        allOk = false;
                        c.Add(Fail("SWP-03", cat, $"sweep_parameters.{prop.Name}", $"Sweep '{prop.Name}' ({t}): requires 'from', 'to', 'step'."));
                    }
                    else
                        c.Add(Pass("SWP-03", cat, $"sweep_parameters.{prop.Name}", $"Sweep '{prop.Name}' ({t}): from/to/step all present."));
                }
                else if (t == "enum_list")
                {
                    if (!prop.Value.TryGetProperty("values", out _))
                    {
                        allOk = false;
                        c.Add(Fail("SWP-03", cat, $"sweep_parameters.{prop.Name}", $"Sweep '{prop.Name}' (enum_list): requires 'values'."));
                    }
                    else
                        c.Add(Pass("SWP-03", cat, $"sweep_parameters.{prop.Name}", $"Sweep '{prop.Name}' (enum_list): 'values' present."));
                }
                else if (t == "gaussian")
                {
                    bool okM = prop.Value.TryGetProperty("mean", out _);
                    bool okS2 = prop.Value.TryGetProperty("std", out _);
                    if (!okM || !okS2)
                    {
                        allOk = false;
                        c.Add(Fail("SWP-03", cat, $"sweep_parameters.{prop.Name}", $"Sweep '{prop.Name}' (gaussian): requires 'mean' and 'std'."));
                    }
                    else
                        c.Add(Pass("SWP-03", cat, $"sweep_parameters.{prop.Name}", $"Sweep '{prop.Name}' (gaussian): mean/std present."));
                }
            }
            if (allOk) c.Add(Pass("SWP-01", cat, "sweep_parameters", "All sweep_parameters entries are valid."));
        }
        else if (!Has(root, "sweep_parameters"))
            c.Add(Info("SWP-01", cat, "sweep_parameters", "Optional block 'sweep_parameters' not provided."));
        else
            c.Add(Fail("SWP-01", cat, "sweep_parameters", "sweep_parameters must be an object."));
        CheckEnum(root, "SWP-04", cat, "combinatorial_strategy",
            new[] { "cartesian", "pairwise", "twise", "random_sample", "guided_by_search" }, c);
        if (Nav(root, "max_generated_scenarios", out var maxS))
            c.Add(maxS.ValueKind == JsonValueKind.Number && maxS.TryGetInt32(out var ms) && ms >= 1
                ? Pass("SWP-05", cat, "max_generated_scenarios", $"max_generated_scenarios = {ms}.")
                : Fail("SWP-05", cat, "max_generated_scenarios", "max_generated_scenarios must be an integer >= 1."));
        else
            c.Add(Info("SWP-05", cat, "max_generated_scenarios", "Optional field 'max_generated_scenarios' not provided."));
        CheckEnum(root, "SWP-06", cat, "selection_strategy",
            new[] { "all", "filter_by_constraints", "search_select_topK" }, c);
        CheckEnum(root, "SWP-07", cat, "search_algorithm.name",
            new[] { "none", "GA", "PSO", "BayesianOpt", "RandomForest-active", "RL" }, c);
        if (Has(root, "search_algorithm.params"))
            c.Add(Obj(root, "search_algorithm.params", out _)
                ? Pass("SWP-08", cat, "search_algorithm.params", "search_algorithm.params is a valid object.")
                : Fail("SWP-08", cat, "search_algorithm.params", "search_algorithm.params must be an object."));
        else if (!Has(root, "search_algorithm"))
            c.Add(Info("SWP-08", cat, "search_algorithm.params", "Optional field 'search_algorithm.params' not provided."));
    }
    private static void RunReporting(JsonElement root, List<ValidationCheck> c)
    {
        const string cat = "Reporting";
        if (!Has(root, "reporting"))
        {
            c.Add(Info("RPT-01", cat, "reporting", "Optional block 'reporting' not provided. RPT-01 to RPT-06 skipped."));
            return;
        }
        CheckEnum(root, "RPT-01", cat, "reporting.log_level", new[] { "ERROR", "WARN", "INFO", "DEBUG" }, c);
        if (Arr(root, "reporting.signals_to_record", out var str))
            c.Add(Pass("RPT-02", cat, "reporting.signals_to_record", $"signals_to_record has {str.GetArrayLength()} entry/entries."));
        else if (!Has(root, "reporting.signals_to_record"))
            c.Add(Info("RPT-02", cat, "reporting.signals_to_record", "Optional field 'signals_to_record' not provided."));
        else
            c.Add(Fail("RPT-02", cat, "reporting.signals_to_record", "signals_to_record must be an array."));
        if (Num(root, "reporting.telemetry_frequency_hz", out var tfhz))
            c.Add(tfhz >= 1 ? Pass("RPT-03", cat, "reporting.telemetry_frequency_hz", $"telemetry_frequency_hz = {tfhz} Hz.")
                            : Fail("RPT-03", cat, "reporting.telemetry_frequency_hz", "telemetry_frequency_hz must be >= 1."));
        else if (!Has(root, "reporting.telemetry_frequency_hz"))
            c.Add(Info("RPT-03", cat, "reporting.telemetry_frequency_hz", "Optional field 'telemetry_frequency_hz' not provided."));
        else
            c.Add(Fail("RPT-03", cat, "reporting.telemetry_frequency_hz", "telemetry_frequency_hz must be a number."));
        if (Has(root, "reporting.trace_capture"))
        {
            Nav(root, "reporting.trace_capture", out var tc);
            c.Add(tc.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? Pass("RPT-04", cat, "reporting.trace_capture", $"trace_capture = {tc.GetBoolean()}.")
                : Fail("RPT-04", cat, "reporting.trace_capture", "trace_capture must be a boolean."));
        }
        else
            c.Add(Info("RPT-04", cat, "reporting.trace_capture", "Optional field 'trace_capture' not provided."));
        CheckEnum(root, "RPT-05", cat, "reporting.report_format", new[] { "HTML", "JSON", "CSV", "PDF" }, c);
        if (Has(root, "reporting.save_artifacts"))
        {
            Nav(root, "reporting.save_artifacts", out var sa);
            c.Add(sa.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? Pass("RPT-06", cat, "reporting.save_artifacts", $"save_artifacts = {sa.GetBoolean()}.")
                : Fail("RPT-06", cat, "reporting.save_artifacts", "save_artifacts must be a boolean."));
        }
        else
            c.Add(Info("RPT-06", cat, "reporting.save_artifacts", "Optional field 'save_artifacts' not provided."));
    }
    private static void RunExecution(JsonElement root, List<ValidationCheck> c)
    {
        const string cat = "Execution";
        if (!Has(root, "execution"))
        {
            c.Add(Info("EXE-01", cat, "execution", "Optional block 'execution' not provided. EXE-01 to EXE-04 skipped."));
            return;
        }
        CheckEnum(root, "EXE-01", cat, "execution.target_runner",
            new[] { "CARLA", "SVL", "SIL_runner", "HIL_bench", "pytest_harness" }, c);
        if (Str(root, "execution.ci_webhook_url", out var wh))
            c.Add(IsUri(wh) ? Pass("EXE-02", cat, "execution.ci_webhook_url", "ci_webhook_url is a valid URI.")
                            : Fail("EXE-02", cat, "execution.ci_webhook_url", $"'{wh}' is not a valid URI."));
        else if (!Has(root, "execution.ci_webhook_url"))
            c.Add(Info("EXE-02", cat, "execution.ci_webhook_url", "Optional field 'ci_webhook_url' not provided."));
        else
            c.Add(Fail("EXE-02", cat, "execution.ci_webhook_url", "ci_webhook_url must be a string."));
        c.Add(Has(root, "execution.docker_image")
            ? Pass("EXE-03", cat, "execution.docker_image", "docker_image is present.")
            : Info("EXE-03", cat, "execution.docker_image", "Optional field 'docker_image' not provided."));
        if (Num(root, "execution.timeout_ms", out var to))
            c.Add(to >= 0 ? Pass("EXE-04", cat, "execution.timeout_ms", $"timeout_ms = {to} ms.")
                          : Fail("EXE-04", cat, "execution.timeout_ms", "timeout_ms must be >= 0."));
        else if (!Has(root, "execution.timeout_ms"))
            c.Add(Info("EXE-04", cat, "execution.timeout_ms", "Optional field 'timeout_ms' not provided."));
        else
            c.Add(Fail("EXE-04", cat, "execution.timeout_ms", "timeout_ms must be a number."));
    }
    private static void RunAdvanced(JsonElement root, List<ValidationCheck> c)
    {
        const string cat = "Advanced";
        if (!Has(root, "advanced"))
        {
            c.Add(Info("ADV-01", cat, "advanced", "Optional block 'advanced' not provided. ADV-01 to ADV-04 skipped."));
            return;
        }
        if (Arr(root, "advanced.constraint_expressions", out var ce))
            c.Add(Pass("ADV-01", cat, "advanced.constraint_expressions", $"constraint_expressions has {ce.GetArrayLength()} expression(s)."));
        else if (!Has(root, "advanced.constraint_expressions"))
            c.Add(Info("ADV-01", cat, "advanced.constraint_expressions", "Optional field 'constraint_expressions' not provided."));
        else
            c.Add(Fail("ADV-01", cat, "advanced.constraint_expressions", "constraint_expressions must be an array."));
        c.Add(Has(root, "advanced.dependency_graph")
            ? Pass("ADV-02", cat, "advanced.dependency_graph", "dependency_graph is present.")
            : Info("ADV-02", cat, "advanced.dependency_graph", "Optional field 'dependency_graph' not provided."));
        if (Str(root, "advanced.seed_dataset_link", out var sdl))
            c.Add(IsUri(sdl) ? Pass("ADV-03", cat, "advanced.seed_dataset_link", "seed_dataset_link is a valid URI.")
                             : Fail("ADV-03", cat, "advanced.seed_dataset_link", $"'{sdl}' is not a valid URI."));
        else if (!Has(root, "advanced.seed_dataset_link"))
            c.Add(Info("ADV-03", cat, "advanced.seed_dataset_link", "Optional field 'seed_dataset_link' not provided."));
        else
            c.Add(Fail("ADV-03", cat, "advanced.seed_dataset_link", "seed_dataset_link must be a string."));
        c.Add(Has(root, "advanced.provenance")
            ? Pass("ADV-04", cat, "advanced.provenance", "provenance is present.")
            : Info("ADV-04", cat, "advanced.provenance", "Optional field 'provenance' not provided."));
    }
    private static void RunCatalog(JsonElement root, List<ValidationCheck> c)
    {
        const string cat = "ECU Catalog";
        if (!Obj(root, "ecu_catalog_entry", out var ece))
        {
            c.Add(Info("CAT-01", cat, "ecu_catalog_entry", "Optional block 'ecu_catalog_entry' not provided. CAT-01 to CAT-11 skipped."));
            return;
        }
        void Chk(string id, string field) =>
            c.Add(Has(ece, field) ? Pass(id, cat, $"ecu_catalog_entry.{field}", $"{field} is present.")
                                  : Info(id, cat, $"ecu_catalog_entry.{field}", $"Optional field '{field}' not provided."));
        Chk("CAT-01", "ecu_id");
        Chk("CAT-02", "ecu_name");
        Chk("CAT-03", "manufacturer");
        if (Arr(ece, "supported_interfaces", out var si))
            c.Add(Pass("CAT-04", cat, "ecu_catalog_entry.supported_interfaces", $"supported_interfaces has {si.GetArrayLength()} entry/entries."));
        else if (!Has(ece, "supported_interfaces"))
            c.Add(Info("CAT-04", cat, "ecu_catalog_entry.supported_interfaces", "Optional field 'supported_interfaces' not provided."));
        else
            c.Add(Fail("CAT-04", cat, "ecu_catalog_entry.supported_interfaces", "supported_interfaces must be an array."));
        if (Num(ece, "min_operational_voltage", out _))
            c.Add(Pass("CAT-05", cat, "ecu_catalog_entry.min_operational_voltage", "min_operational_voltage is present."));
        else if (!Has(ece, "min_operational_voltage"))
            c.Add(Info("CAT-05", cat, "ecu_catalog_entry.min_operational_voltage", "Optional field 'min_operational_voltage' not provided."));
        else
            c.Add(Fail("CAT-05", cat, "ecu_catalog_entry.min_operational_voltage", "min_operational_voltage must be a number."));
        if (Num(ece, "max_operational_voltage", out _))
            c.Add(Pass("CAT-06", cat, "ecu_catalog_entry.max_operational_voltage", "max_operational_voltage is present."));
        else if (!Has(ece, "max_operational_voltage"))
            c.Add(Info("CAT-06", cat, "ecu_catalog_entry.max_operational_voltage", "Optional field 'max_operational_voltage' not provided."));
        else
            c.Add(Fail("CAT-06", cat, "ecu_catalog_entry.max_operational_voltage", "max_operational_voltage must be a number."));
        if (Arr(ece, "supported_modes", out var sm))
            c.Add(Pass("CAT-07", cat, "ecu_catalog_entry.supported_modes", $"supported_modes has {sm.GetArrayLength()} entry/entries."));
        else if (!Has(ece, "supported_modes"))
            c.Add(Info("CAT-07", cat, "ecu_catalog_entry.supported_modes", "Optional field 'supported_modes' not provided."));
        else
            c.Add(Fail("CAT-07", cat, "ecu_catalog_entry.supported_modes", "supported_modes must be an array."));
        if (Arr(ece, "firmware_versions", out var fv))
            c.Add(Pass("CAT-08", cat, "ecu_catalog_entry.firmware_versions", $"firmware_versions has {fv.GetArrayLength()} entry/entries."));
        else if (!Has(ece, "firmware_versions"))
            c.Add(Info("CAT-08", cat, "ecu_catalog_entry.firmware_versions", "Optional field 'firmware_versions' not provided."));
        else
            c.Add(Fail("CAT-08", cat, "ecu_catalog_entry.firmware_versions", "firmware_versions must be an array."));
        if (Arr(ece, "default_assertions", out var da))
            c.Add(Pass("CAT-09", cat, "ecu_catalog_entry.default_assertions", $"default_assertions has {da.GetArrayLength()} entry/entries."));
        else if (!Has(ece, "default_assertions"))
            c.Add(Info("CAT-09", cat, "ecu_catalog_entry.default_assertions", "Optional field 'default_assertions' not provided."));
        else
            c.Add(Fail("CAT-09", cat, "ecu_catalog_entry.default_assertions", "default_assertions must be an array."));
        Chk("CAT-10", "calibration_requirements");
        Chk("CAT-11", "certification_level");
    }
    private static void RunSchema(JsonElement root, List<ValidationCheck> c)
    {
        const string cat = "Schema";
        var unknown = root.EnumerateObject()
            .Where(p => !s_knownRoot.Contains(p.Name))
            .Select(p => p.Name)
            .ToList();
        c.Add(unknown.Count == 0
            ? Pass("SCH-01", cat, "(root)", "No unknown root-level properties found.")
            : Fail("SCH-01", cat, "(root)", $"Unknown root-level property/properties: {string.Join(", ", unknown)}."));
    }
    private static void RunSecurity(JsonElement root, List<ValidationCheck> c)
    {
        const string cat = "Security";
        if (!Has(root, "security"))
        {
            c.Add(Info("SEC-01", cat, "security", "Optional block 'security' not provided. SEC-01 to SEC-05 skipped."));
            return;
        }
        CheckEnum(root, "SEC-01", cat, "security.attack_type",
            new[] { "spoofing", "replay", "DoS", "fuzzing", "man_in_middle" }, c);
        c.Add(Has(root, "security.attack_vector")
            ? Pass("SEC-02", cat, "security.attack_vector", "attack_vector is present.")
            : Info("SEC-02", cat, "security.attack_vector", "Optional field 'attack_vector' not provided."));
        c.Add(Has(root, "security.privilege_level")
            ? Pass("SEC-03", cat, "security.privilege_level", "privilege_level is present.")
            : Info("SEC-03", cat, "security.privilege_level", "Optional field 'privilege_level' not provided."));
        if (Num(root, "security.start_time_ms", out var ss))
            c.Add(ss >= 0 ? Pass("SEC-04", cat, "security.start_time_ms", $"start_time_ms = {ss} ms.")
                          : Fail("SEC-04", cat, "security.start_time_ms", "security.start_time_ms must be >= 0."));
        else if (!Has(root, "security.start_time_ms"))
            c.Add(Info("SEC-04", cat, "security.start_time_ms", "Optional field 'security.start_time_ms' not provided."));
        else
            c.Add(Fail("SEC-04", cat, "security.start_time_ms", "security.start_time_ms must be a number."));
        if (Num(root, "security.duration_ms", out var sd))
            c.Add(sd >= 0 ? Pass("SEC-05", cat, "security.duration_ms", $"duration_ms = {sd} ms.")
                          : Fail("SEC-05", cat, "security.duration_ms", "security.duration_ms must be >= 0."));
        else if (!Has(root, "security.duration_ms"))
            c.Add(Info("SEC-05", cat, "security.duration_ms", "Optional field 'security.duration_ms' not provided."));
        else
            c.Add(Fail("SEC-05", cat, "security.duration_ms", "security.duration_ms must be a number."));
    }
    private static void RunTestTypes(JsonElement root, List<ValidationCheck> c)
    {
        const string cat = "Test-Type-Specific";
        if (Has(root, "unit_test"))
        {
            if (Arr(root, "unit_test.input_sequence", out _))
                c.Add(Pass("TTS-01", cat, "unit_test.input_sequence", "input_sequence is present."));
            else if (!Has(root, "unit_test.input_sequence"))
                c.Add(Info("TTS-01", cat, "unit_test.input_sequence", "Optional field 'unit_test.input_sequence' not provided."));
            else
                c.Add(Fail("TTS-01", cat, "unit_test.input_sequence", "input_sequence must be an array."));
            if (Num(root, "unit_test.measurement_window_ms", out var mw))
                c.Add(mw >= 0 ? Pass("TTS-02", cat, "unit_test.measurement_window_ms", $"measurement_window_ms = {mw} ms.")
                              : Fail("TTS-02", cat, "unit_test.measurement_window_ms", "measurement_window_ms must be >= 0."));
            else if (!Has(root, "unit_test.measurement_window_ms"))
                c.Add(Info("TTS-02", cat, "unit_test.measurement_window_ms", "Optional field 'unit_test.measurement_window_ms' not provided."));
            else
                c.Add(Fail("TTS-02", cat, "unit_test.measurement_window_ms", "measurement_window_ms must be a number."));
            if (Num(root, "unit_test.steady_state_tolerance", out var sst))
                c.Add(sst >= 0 ? Pass("TTS-03", cat, "unit_test.steady_state_tolerance", $"steady_state_tolerance = {sst}.")
                               : Fail("TTS-03", cat, "unit_test.steady_state_tolerance", "steady_state_tolerance must be >= 0."));
            else if (!Has(root, "unit_test.steady_state_tolerance"))
                c.Add(Info("TTS-03", cat, "unit_test.steady_state_tolerance", "Optional field 'unit_test.steady_state_tolerance' not provided."));
            else
                c.Add(Fail("TTS-03", cat, "unit_test.steady_state_tolerance", "steady_state_tolerance must be a number."));
            if (Num(root, "unit_test.overshoot_limit", out var ol))
                c.Add(ol >= 0 ? Pass("TTS-04", cat, "unit_test.overshoot_limit", $"overshoot_limit = {ol}.")
                              : Fail("TTS-04", cat, "unit_test.overshoot_limit", "overshoot_limit must be >= 0."));
            else if (!Has(root, "unit_test.overshoot_limit"))
                c.Add(Info("TTS-04", cat, "unit_test.overshoot_limit", "Optional field 'unit_test.overshoot_limit' not provided."));
            else
                c.Add(Fail("TTS-04", cat, "unit_test.overshoot_limit", "overshoot_limit must be a number."));
        }
        else
            c.Add(Info("TTS-01", cat, "unit_test", "Block 'unit_test' not present. TTS-01 to TTS-04 skipped."));
        if (Has(root, "integration_test"))
        {
            c.Add(Has(root, "integration_test.connectivity_topology")
                ? Pass("TTS-05", cat, "integration_test.connectivity_topology", "connectivity_topology is present.")
                : Info("TTS-05", cat, "integration_test.connectivity_topology", "Optional field 'connectivity_topology' not provided."));
            c.Add(Has(root, "integration_test.gateway_behaviour")
                ? Pass("TTS-06", cat, "integration_test.gateway_behaviour", "gateway_behaviour is present.")
                : Info("TTS-06", cat, "integration_test.gateway_behaviour", "Optional field 'gateway_behaviour' not provided."));
        }
        else
            c.Add(Info("TTS-05", cat, "integration_test", "Block 'integration_test' not present. TTS-05 to TTS-06 skipped."));
        if (Has(root, "stress_test"))
        {
            if (Num(root, "stress_test.duration_hours", out var dh))
                c.Add(dh > 0 ? Pass("TTS-07", cat, "stress_test.duration_hours", $"duration_hours = {dh}.")
                             : Fail("TTS-07", cat, "stress_test.duration_hours", "stress_test.duration_hours must be > 0."));
            else if (!Has(root, "stress_test.duration_hours"))
                c.Add(Info("TTS-07", cat, "stress_test.duration_hours", "Optional field 'stress_test.duration_hours' not provided."));
            else
                c.Add(Fail("TTS-07", cat, "stress_test.duration_hours", "duration_hours must be a number."));
            c.Add(Has(root, "stress_test.high_temperature_profile")
                ? Pass("TTS-08", cat, "stress_test.high_temperature_profile", "high_temperature_profile is present.")
                : Info("TTS-08", cat, "stress_test.high_temperature_profile", "Optional field 'high_temperature_profile' not provided."));
            c.Add(Has(root, "stress_test.battery_drain_model")
                ? Pass("TTS-09", cat, "stress_test.battery_drain_model", "battery_drain_model is present.")
                : Info("TTS-09", cat, "stress_test.battery_drain_model", "Optional field 'battery_drain_model' not provided."));
            c.Add(Has(root, "stress_test.background_load_pattern")
                ? Pass("TTS-10", cat, "stress_test.background_load_pattern", "background_load_pattern is present.")
                : Info("TTS-10", cat, "stress_test.background_load_pattern", "Optional field 'background_load_pattern' not provided."));
        }
        else
            c.Add(Info("TTS-07", cat, "stress_test", "Block 'stress_test' not present. TTS-07 to TTS-10 skipped."));
    }
    private static void RunCrossFields(JsonElement root, List<ValidationCheck> c)
    {
        const string cat = "Cross-Field";
        if (Str(root, "initial_conditions.engine_state", out var engState)
            && Num(root, "initial_conditions.vehicle_speed_mps", out var speed))
            c.Add(engState == "OFF" && speed > 0
                ? Fail("XVAL-01", cat, "initial_conditions.vehicle_speed_mps", $"Engine is OFF but vehicle_speed_mps = {speed}. Forbidden unless tow_mode is enabled.")
                : Pass("XVAL-01", cat, "initial_conditions.vehicle_speed_mps", "Engine state and vehicle speed are consistent."));
        if (Str(root, "initial_conditions.gear_position", out var gear)
            && Num(root, "initial_conditions.vehicle_speed_mps", out var speed2))
            c.Add(gear == "P" && speed2 > 0
                ? Fail("XVAL-02", cat, "initial_conditions.vehicle_speed_mps", $"Gear is in Park but vehicle_speed_mps = {speed2}. Physically impossible.")
                : Pass("XVAL-02", cat, "initial_conditions.vehicle_speed_mps", "Gear position and vehicle speed are consistent."));
        if (Str(root, "initial_conditions.ignition_key", out var ign)
            && Str(root, "ecu_power_state", out var pwr))
        {
            if (ign == "OFF" && pwr == "ON")
            {
                Str(root, "target_execution", out var te2);
                c.Add(te2 == "bench"
                    ? Warn("XVAL-03", cat, "ecu_power_state", "Ignition is OFF but ECU is ON. Allowed on bench (external power supply assumed). Verify this is intentional.")
                    : Fail("XVAL-03", cat, "ecu_power_state", $"Ignition is OFF but ECU is ON. Only allowed when target_execution is 'bench' (currently '{te2}')."));
            }
            else
                c.Add(Pass("XVAL-03", cat, "ecu_power_state", "Ignition key and ECU power state are consistent."));
        }
        if (Num(root, "initial_conditions.battery_voltage_V", out var batt)
            && Num(root, "ecu_catalog_entry.min_operational_voltage", out var minV))
            c.Add(batt < minV
                ? Fail("XVAL-04", cat, "initial_conditions.battery_voltage_V", $"Battery voltage {batt}V is below ECU minimum operational voltage {minV}V.")
                : Pass("XVAL-04", cat, "initial_conditions.battery_voltage_V", $"Battery voltage {batt}V >= ECU minimum {minV}V."));
        if (Arr(root, "faults", out var faultsX) && (Arr(root, "bus_config.message_defs", out _) || Arr(root, "signal_list", out _)))
        {
            var knownTargets = new HashSet<string>();
            if (Arr(root, "bus_config.message_defs", out var mds))
                foreach (var m in mds.EnumerateArray())
                {
                    if (m.TryGetProperty("id", out var mid))   knownTargets.Add(mid.ToString());
                    if (m.TryGetProperty("name", out var mn2)) knownTargets.Add(mn2.ToString());
                }
            if (Arr(root, "signal_list", out var sl))
                foreach (var s in sl.EnumerateArray())
                    if (s.TryGetProperty("name", out var sn)) knownTargets.Add(sn.ToString());
            int fi2 = 0;
            foreach (var fault in faultsX.EnumerateArray())
            {
                if (fault.TryGetProperty("target", out var tgt))
                {
                    var tgtStr = tgt.ToString();
                    c.Add(knownTargets.Contains(tgtStr)
                        ? Pass("XVAL-05", cat, $"faults[{fi2}].target", $"Fault target '{tgtStr}' references a known message/signal.")
                        : Fail("XVAL-05", cat, $"faults[{fi2}].target", $"Fault target '{tgtStr}' does not match any message id/name or signal name."));
                }
                fi2++;
            }
        }
        if (Arr(root, "faults", out var faultsT) && Num(root, "timing.test_duration_ms", out var testDur))
        {
            int fi3 = 0;
            foreach (var fault in faultsT.EnumerateArray())
            {
                if (fault.TryGetProperty("start_time_ms", out var stms) && stms.ValueKind == JsonValueKind.Number
                    && fault.TryGetProperty("duration_ms", out var dms2) && dms2.ValueKind == JsonValueKind.Number)
                {
                    var endTime = stms.GetDouble() + dms2.GetDouble();
                    c.Add(endTime > testDur
                        ? Fail("XVAL-06", cat, $"faults[{fi3}]", $"Fault {fi3}: end time ({endTime} ms) exceeds test_duration_ms ({testDur} ms).")
                        : Pass("XVAL-06", cat, $"faults[{fi3}]", $"Fault {fi3}: timing is within test duration."));
                }
                fi3++;
            }
        }
        if (Arr(root, "assertions", out var assrX) && Num(root, "timing.test_duration_ms", out var testDur2))
        {
            int ai2 = 0;
            foreach (var asr in assrX.EnumerateArray())
            {
                if (asr.TryGetProperty("time_window_ms", out var tw)
                    && tw.TryGetProperty("to", out var toEl) && toEl.ValueKind == JsonValueKind.Number)
                {
                    var toVal = toEl.GetDouble();
                    c.Add(toVal > testDur2
                        ? Fail("XVAL-07", cat, $"assertions[{ai2}].time_window_ms.to", $"Assertion {ai2}: time_window_ms.to ({toVal}) > test_duration_ms ({testDur2}). Suggest auto-clipping.")
                        : Pass("XVAL-07", cat, $"assertions[{ai2}].time_window_ms.to", $"Assertion {ai2}: time_window_ms.to is within test duration."));
                }
                ai2++;
            }
        }
        if (Str(root, "safety.ASIL_level", out var asil) && Arr(root, "assertions", out var assrD))
        {
            if (asil == "D")
            {
                bool hasCritical = assrD.EnumerateArray()
                    .Any(a => a.TryGetProperty("severity", out var sv) && sv.GetString() == "critical");
                c.Add(hasCritical
                    ? Pass("XVAL-08", cat, "assertions", "At least one critical-severity assertion found for ASIL D.")
                    : Warn("XVAL-08", cat, "assertions", "ASIL D specified but no assertion with severity='critical' found. Add at least one critical assertion."));
                bool hasFormal = assrD.EnumerateArray()
                    .Any(a => a.TryGetProperty("type", out var at2) && (at2.GetString() == "STL" || at2.GetString() == "LTL"));
                c.Add(hasFormal
                    ? Pass("XVAL-09", cat, "assertions", "At least one STL/LTL formal assertion found for ASIL D.")
                    : Warn("XVAL-09", cat, "assertions", "ASIL D specified but no STL or LTL formal assertion found."));
            }
        }
        if (Str(root, "safety.ASIL_level", out var asilCE) && (asilCE == "C" || asilCE == "D"))
        {
            if (Str(root, "target_execution", out var te3) && te3 == "simulation")
                c.Add(Warn("XVAL-10", cat, "target_execution", $"ASIL {asilCE} with target_execution='simulation'. For ASIL C/D, prefer HIL or lab to avoid qualification gaps."));
            else if (Str(root, "target_execution", out _))
                c.Add(Pass("XVAL-10", cat, "target_execution", $"ASIL {asilCE} with non-simulation target execution is acceptable."));
        }
        if (Str(root, "safety.ASIL_level", out var asilH) && !Has(root, "safety.hazard_id"))
            c.Add(Warn("XVAL-11", cat, "safety.hazard_id", $"ASIL level '{asilH}' is set but 'hazard_id' is missing. Link to a hazard register entry."));
        else if (Str(root, "safety.ASIL_level", out _) && Has(root, "safety.hazard_id"))
            c.Add(Pass("XVAL-11", cat, "safety.hazard_id", "ASIL level is set and hazard_id is linked."));
        if (Str(root, "ecu_mode", out var ecuMode) && ecuMode == "CALIBRATION"
            && Str(root, "initial_conditions.ignition_key", out var ignCal) && ignCal == "OFF")
        {
            Str(root, "target_execution", out var teC);
            c.Add(teC == "bench"
                ? Pass("XVAL-12", cat, "ecu_mode", "CALIBRATION with ignition OFF is allowed on bench.")
                : Fail("XVAL-12", cat, "ecu_mode", $"CALIBRATION mode requires ignition ON (currently OFF). Not on bench (target_execution='{teC}')."));
        }
        if (Str(root, "bus_config.bus_type", out var busTypeX) && busTypeX == "FlexRay"
            && Num(root, "bus_config.bitrate_kbps", out var brX))
        {
            var allowed = new HashSet<double> { 2500, 5000, 10000 };
            c.Add(allowed.Contains(brX)
                ? Pass("XVAL-13", cat, "bus_config.bitrate_kbps", $"FlexRay bitrate {brX} kbps is a valid FlexRay bitrate.")
                : Fail("XVAL-13", cat, "bus_config.bitrate_kbps", $"FlexRay bitrate {brX} kbps is invalid. Must be 2500, 5000, or 10000."));
        }
        if (Num(root, "bus_config.bus_load_pct", out var busLoad))
        {
            if (busLoad > 100)
                c.Add(Fail("XVAL-14", cat, "bus_config.bus_load_pct", $"bus_load_pct = {busLoad} exceeds 100%. This is physically impossible."));
            else if (busLoad > 80)
                c.Add(Warn("XVAL-15", cat, "bus_config.bus_load_pct", $"bus_load_pct = {busLoad}% is very high (> 80%). May cause timing violations and dropped messages."));
            else
                c.Add(Pass("XVAL-15", cat, "bus_config.bus_load_pct", $"bus_load_pct = {busLoad}% is acceptable."));
        }
        if (Arr(root, "faults", out var faultsSS))
        {
            int fiSS = 0;
            foreach (var fault in faultsSS.EnumerateArray())
            {
                if (fault.TryGetProperty("type", out var ft2) && ft2.GetString() == "sensor_spoof"
                    && fault.TryGetProperty("intensity", out var inten2) && inten2.ValueKind == JsonValueKind.Number
                    && inten2.GetDouble() > 1.0)
                    c.Add(Fail("XVAL-16", cat, $"faults[{fiSS}].intensity", $"sensor_spoof fault {fiSS}: intensity {inten2.GetDouble()} exceeds 1.0."));
                fiSS++;
            }
        }
        if (Arr(root, "faults", out var faultsAS) && Arr(root, "signal_list", out var sigListAS))
        {
            var sigRanges = new Dictionary<string, (double min, double max)>();
            foreach (var s in sigListAS.EnumerateArray())
            {
                if (s.TryGetProperty("name", out var sn) && sn.ValueKind == JsonValueKind.String
                    && s.TryGetProperty("min", out var smin) && smin.ValueKind == JsonValueKind.Number
                    && s.TryGetProperty("max", out var smax) && smax.ValueKind == JsonValueKind.Number)
                    sigRanges[sn.GetString()!] = (smin.GetDouble(), smax.GetDouble());
            }
            int fiAS = 0;
            foreach (var fault in faultsAS.EnumerateArray())
            {
                if (fault.TryGetProperty("type", out var ft3) && ft3.GetString() == "actuator_stuck"
                    && fault.TryGetProperty("target", out var tgt2) && tgt2.ValueKind == JsonValueKind.String
                    && fault.TryGetProperty("position", out var pos) && pos.ValueKind == JsonValueKind.Number)
                {
                    var tgtName = tgt2.GetString()!;
                    if (sigRanges.TryGetValue(tgtName, out var rng))
                    {
                        var pv = pos.GetDouble();
                        c.Add(pv >= rng.min && pv <= rng.max
                            ? Pass("XVAL-17", cat, $"faults[{fiAS}].position", $"actuator_stuck position {pv} is within signal '{tgtName}' range [{rng.min}..{rng.max}].")
                            : Fail("XVAL-17", cat, $"faults[{fiAS}].position", $"actuator_stuck position {pv} is outside signal '{tgtName}' range [{rng.min}..{rng.max}]."));
                    }
                }
                fiAS++;
            }
        }
        if (Num(root, "initial_conditions.fuel_level_percent", out var fuel) && fuel == 0)
        {
            bool isColdStart = false;
            if (Str(root, "profile.name", out var pname) && pname.Contains("cold_start", StringComparison.OrdinalIgnoreCase))
                isColdStart = true;
            if (Has(root, "unit_test") && Str(root, "unit_test.input_sequence", out var _))
                isColdStart = false;
            if (isColdStart)
                c.Add(Fail("XVAL-18", cat, "initial_conditions.fuel_level_percent", "fuel_level_percent is 0 but profile indicates a cold_start test. Cannot cold-start with no fuel."));
        }
        if (Str(root, "combinatorial_strategy", out var cs) && cs == "cartesian"
            && Obj(root, "sweep_parameters", out var spX)
            && Nav(root, "max_generated_scenarios", out var mgsEl) && mgsEl.ValueKind == JsonValueKind.Number)
        {
            double product = 1;
            foreach (var prop in spX.EnumerateObject())
            {
                if (!prop.Value.TryGetProperty("type", out var st)) continue;
                var stStr = st.GetString();
                if (stStr == "enum_list" && prop.Value.TryGetProperty("values", out var vl) && vl.ValueKind == JsonValueKind.Array)
                    product *= vl.GetArrayLength();
                else if ((stStr == "linear_range" || stStr == "geometric_range")
                         && prop.Value.TryGetProperty("from", out var frm) && frm.ValueKind == JsonValueKind.Number
                         && prop.Value.TryGetProperty("to", out var toE) && toE.ValueKind == JsonValueKind.Number
                         && prop.Value.TryGetProperty("step", out var step) && step.ValueKind == JsonValueKind.Number
                         && step.GetDouble() > 0)
                    product *= Math.Floor((toE.GetDouble() - frm.GetDouble()) / step.GetDouble()) + 1;
                else
                    product *= 10;
            }
            var mgs = mgsEl.GetInt32();
            c.Add(product > mgs
                ? Warn("XVAL-19", cat, "combinatorial_strategy", $"Estimated cartesian product ({product:0}) exceeds max_generated_scenarios ({mgs}). Consider switching to 'pairwise' or 'guided_by_search'.")
                : Pass("XVAL-19", cat, "combinatorial_strategy", $"Estimated cartesian product ({product:0}) is within max_generated_scenarios ({mgs})."));
        }
        if (Str(root, "combinatorial_strategy", out var csR) && csR == "random_sample")
        {
            bool hasSeed = Has(root, "search_algorithm.params")
                           && Obj(root, "search_algorithm.params", out var saParams)
                           && Has(saParams, "seed");
            c.Add(hasSeed
                ? Pass("XVAL-20", cat, "search_algorithm.params", "Random sample has a seed — results will be reproducible.")
                : Warn("XVAL-20", cat, "search_algorithm.params", "random_sample strategy without a seed in search_algorithm.params. Results will not be reproducible."));
        }
        if (Str(root, "target_execution", out var teX) && teX == "HIL")
        {
            if (Str(root, "execution.target_runner", out var tr))
                c.Add(tr == "HIL_bench"
                    ? Pass("XVAL-21", cat, "execution.target_runner", "target_runner is correctly set to 'HIL_bench' for HIL execution.")
                    : Fail("XVAL-21", cat, "execution.target_runner", $"target_execution is 'HIL' but target_runner is '{tr}'. Must be 'HIL_bench'."));
        }
        if (Arr(root, "faults", out var faultsOv))
        {
            var faultList = faultsOv.EnumerateArray().Select(f =>
            {
                f.TryGetProperty("target", out var tgt3);
                f.TryGetProperty("start_time_ms", out var st2);
                f.TryGetProperty("duration_ms", out var dur2);
                f.TryGetProperty("type", out var ft4);
                return (
                    target: tgt3.ValueKind == JsonValueKind.Undefined ? null : tgt3.ToString(),
                    start: st2.ValueKind == JsonValueKind.Number ? st2.GetDouble() : (double?)null,
                    end: st2.ValueKind == JsonValueKind.Number && dur2.ValueKind == JsonValueKind.Number
                         ? st2.GetDouble() + dur2.GetDouble() : (double?)null,
                    type: ft4.ValueKind == JsonValueKind.Undefined ? null : ft4.GetString()
                );
            }).ToList();
            for (int i = 0; i < faultList.Count; i++)
            {
                for (int j = i + 1; j < faultList.Count; j++)
                {
                    var a = faultList[i]; var b = faultList[j];
                    if (a.target != null && a.target == b.target
                        && a.start.HasValue && a.end.HasValue && b.start.HasValue && b.end.HasValue
                        && a.start < b.end && b.start < a.end
                        && a.type != b.type)
                        c.Add(Fail("XVAL-22", cat, $"faults[{i}]/faults[{j}]",
                            $"Faults {i} ({a.type}) and {j} ({b.type}) target '{a.target}' with overlapping time windows and incompatible types. Merge or separate them."));
                }
            }
        }
        if (Arr(root, "advanced.constraint_expressions", out var exprs))
        {
            var seen = new Dictionary<string, (string op, double val)>(StringComparer.OrdinalIgnoreCase);
            var conflicts = new List<string>();
            foreach (var exprEl in exprs.EnumerateArray())
            {
                if (exprEl.ValueKind != JsonValueKind.String) continue;
                var expr = exprEl.GetString()!.Trim();
                var m = System.Text.RegularExpressions.Regex.Match(expr, @"^(\w+)\s*([<>]=?|==)\s*(-?\d+\.?\d*)$");
                if (!m.Success) continue;
                var varName = m.Groups[1].Value;
                var op      = m.Groups[2].Value;
                var val     = double.Parse(m.Groups[3].Value);
                if (seen.TryGetValue(varName, out var prev))
                {
                    bool conflict = (prev.op.StartsWith(">") && op.StartsWith("<") && prev.val >= val)
                                 || (prev.op.StartsWith("<") && op.StartsWith(">") && val >= prev.val);
                    if (conflict) conflicts.Add($"'{prev.op}{prev.val}' conflicts with '{op}{val}' for variable '{varName}'");
                }
                seen[varName] = (op, val);
            }
            c.Add(conflicts.Count == 0
                ? Pass("XVAL-23", cat, "advanced.constraint_expressions", "No obvious contradictions found in constraint_expressions.")
                : Fail("XVAL-23", cat, "advanced.constraint_expressions", $"Contradictory constraints detected: {string.Join("; ", conflicts)}."));
        }
        if (Str(root, "ecu_id", out var rootEcuId) && Str(root, "ecu_catalog_entry.ecu_id", out var catEcuId))
            c.Add(rootEcuId == catEcuId
                ? Pass("XVAL-24", cat, "ecu_id", $"Root ecu_id '{rootEcuId}' matches ecu_catalog_entry.ecu_id.")
                : Fail("XVAL-24", cat, "ecu_id", $"Root ecu_id '{rootEcuId}' does not match ecu_catalog_entry.ecu_id '{catEcuId}'."));
        if (Obj(root, "sweep_parameters", out var spXV))
        {
            foreach (var prop in spXV.EnumerateObject())
            {
                if (!prop.Value.TryGetProperty("type", out var swT)) continue;
                var swStr = swT.GetString();
                if (swStr is not ("linear_range" or "geometric_range")) continue;
                if (prop.Value.TryGetProperty("from", out var frm2) && frm2.ValueKind == JsonValueKind.Number
                    && prop.Value.TryGetProperty("to", out var toE2) && toE2.ValueKind == JsonValueKind.Number)
                {
                    c.Add(frm2.GetDouble() >= toE2.GetDouble()
                        ? Fail("XVAL-25", cat, $"sweep_parameters.{prop.Name}", $"Sweep '{prop.Name}': from ({frm2.GetDouble()}) >= to ({toE2.GetDouble()}). Range is empty or inverted.")
                        : Pass("XVAL-25", cat, $"sweep_parameters.{prop.Name}", $"Sweep '{prop.Name}': from < to."));
                }
                if (prop.Value.TryGetProperty("step", out var step2) && step2.ValueKind == JsonValueKind.Number)
                {
                    c.Add(step2.GetDouble() <= 0
                        ? Fail("XVAL-26", cat, $"sweep_parameters.{prop.Name}.step", $"Sweep '{prop.Name}': step = {step2.GetDouble()} must be > 0.")
                        : Pass("XVAL-26", cat, $"sweep_parameters.{prop.Name}.step", $"Sweep '{prop.Name}': step = {step2.GetDouble()} is positive."));
                }
            }
        }
        if (Has(root, "security")
            && Num(root, "security.start_time_ms", out var secStart)
            && Num(root, "security.duration_ms", out var secDur)
            && Num(root, "timing.test_duration_ms", out var testDur3))
        {
            var secEnd = secStart + secDur;
            c.Add(secEnd > testDur3
                ? Fail("XVAL-27", cat, "security", $"Security attack ends at {secEnd} ms but test_duration_ms is {testDur3} ms. Attack extends beyond test duration.")
                : Pass("XVAL-27", cat, "security", $"Security attack timing ({secEnd} ms) is within test_duration_ms ({testDur3} ms)."));
        }
    }
}
