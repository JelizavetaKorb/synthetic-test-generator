using System.Text.Json;
using System.Text.Json.Nodes;
using TestGen.Core.Models;
using TestGen.Core.Services;

namespace TestGen.Core.Formatting;

public interface IScenarioFormatter
{
    JsonDocument FormatOne(Scenario scenario, Profile? profile, GenerateRequest request);
    JsonDocument FormatMany(IReadOnlyList<Scenario> scenarios, Profile? profile, GenerateRequest request);
}

public sealed class ScenarioFormatter : IScenarioFormatter
{

    private static readonly Dictionary<string, string> KeyMap =
        new(StringComparer.Ordinal)
        {

            ["ecuId"]                                    = "ecu_id",
            ["ecuMode"]                                  = "ecu_mode",
            ["ecuPowerState"]                            = "ecu_power_state",
            ["ecuFirmwareVersion"]                       = "ecu_firmware_version",
            ["ecuTemperatureC"]                          = "ecu_temperature_C",
            ["hwInterface"]                              = "hw_interface",
            ["diagnosticSession"]                        = "diagnostic_session",
            ["exportFormats"]                            = "export_formats",
            ["targetExecution"]                          = "target_execution",
            ["combinatorialStrategy"]                    = "combinatorial_strategy",
            ["maxGeneratedScenarios"]                    = "max_generated_scenarios",
            ["selectionStrategy"]                        = "selection_strategy",
            ["priority"]                                 = "priority",
            ["author"]                                   = "author",
            ["project"]                                  = "project",
            ["tags"]                                     = "tags",
            ["version"]                                  = "version",

            ["initialConditions.ambientTemperatureC"]    = "ambient_temperature_C",
            ["initialConditions.batteryVoltageV"]        = "battery_voltage_V",
            ["initialConditions.engineState"]            = "engine_state",
            ["initialConditions.fuelLevelPercent"]       = "fuel_level_percent",
            ["initialConditions.vehicleSpeedMps"]        = "vehicle_speed_mps",
            ["initialConditions.gearPosition"]           = "gear_position",
            ["initialConditions.ignitionKey"]            = "ignition_key",
            ["initialConditions.ambientHumidityPct"]     = "ambient_humidity_pct",
            ["initialConditions.gpsFixQuality"]          = "gps_fix_quality",
            ["initialConditions.localizationErrorM"]     = "localization_error_m",

            ["testDurationMs"]                           = "test_duration_ms",
            ["timeResolutionMs"]                         = "time_resolution_ms",
            ["timing.startupTimeoutMs"]                  = "startup_timeout_ms",
            ["timing.warmupDurationS"]                   = "warmup_duration_s",
            ["timing.measurementIntervalMs"]             = "measurement_interval_ms",
            ["timing.testDurationMs"]                    = "test_duration_ms",
            ["timing.timeResolutionMs"]                  = "time_resolution_ms",
            ["timing.repetitions"]                       = "repetitions",
            ["timing.interTestDelayMs"]                  = "inter_test_delay_ms",

            ["bus.protocol"]                             = "bus_type",
            ["bus.baudRateKbps"]                         = "bitrate_kbps",
            ["busType"]                                  = "bus_type",
            ["bitrateKbps"]                              = "bitrate_kbps",
            ["busLoadPct"]                               = "bus_load_pct",
            ["messageJitterMs"]                          = "message_jitter_ms",

            ["reporting.captureSignals"]                 = "signals_to_record",
            ["reporting.captureDtcs"]                    = "capture_dtcs",
            ["reporting.format"]                         = "report_format",
            ["reporting.logLevel"]                       = "log_level",
            ["reporting.telemetryFrequencyHz"]           = "telemetry_frequency_hz",
            ["reporting.traceCapture"]                   = "trace_capture",
            ["reporting.saveArtifacts"]                  = "save_artifacts",

            ["asilLevel"]                                = "ASIL_level",
            ["hazardId"]                                 = "hazard_id",
            ["safetyGoal"]                               = "safety_goal",
            ["riskWeight"]                               = "risk_weight",
            ["recoveryBehaviourExpected"]                = "recovery_behaviour_expected",

            ["faultId"]                                  = "fault_id",
            ["startTimeMs"]                              = "start_time_ms",
            ["durationMs"]                               = "duration_ms",
        };

    private static readonly HashSet<string> SafetyKeys =
        new(StringComparer.Ordinal)
        { "asilLevel", "hazardId", "safetyGoal", "riskWeight", "recoveryBehaviourExpected" };

    private static readonly HashSet<string> TopLevelScalarKeys =
        new(StringComparer.Ordinal)
        {
            "ecuId", "ecuMode", "ecuPowerState", "ecuFirmwareVersion", "ecuTemperatureC",
            "hwInterface", "diagnosticSession", "exportFormats", "targetExecution",
            "combinatorialStrategy", "maxGeneratedScenarios", "selectionStrategy",
            "priority", "author", "project", "tags", "version"
        };

    private static readonly HashSet<string> TimingFlatKeys =
        new(StringComparer.Ordinal)
        { "testDurationMs", "timeResolutionMs" };

    private static readonly HashSet<string> BusFlatKeys =
        new(StringComparer.Ordinal)
        { "busType", "bitrateKbps", "busLoadPct", "messageJitterMs" };

    public JsonDocument FormatOne(Scenario scenario, Profile? profile, GenerateRequest request)
    {
        var p    = NormalizeBracketKeys(scenario.Parameters);
        var root = new JsonObject();

        root["scenario_id"] = scenario.ScenarioId;
        root["name"]        = scenario.Name;
        if (!string.IsNullOrEmpty(scenario.Description))
            root["description"] = scenario.Description;
        root["created_at"]              = DateTime.UtcNow.ToString("o");
        root["combinatorial_strategy"]  = request.Strategy ?? "cartesian";

        foreach (var key in TopLevelScalarKeys)
        {
            if (!p.ContainsKey(key)) continue;
            var outKey = Remap(key);
            root[outKey] = ToJsonNode(p[key]);
        }

        var ic = new JsonObject();
        foreach (var kvp in p)
        {
            if (!kvp.Key.StartsWith("initialConditions.", StringComparison.Ordinal)) continue;
            var outKey = Remap(kvp.Key);
            ic[outKey] = ToJsonNode(kvp.Value);
        }
        if (ic.Count > 0) root["initial_conditions"] = ic;

        var timing = new JsonObject();
        foreach (var kvp in p)
        {
            bool isTiming = kvp.Key.StartsWith("timing.", StringComparison.Ordinal)
                            || TimingFlatKeys.Contains(kvp.Key);
            if (!isTiming) continue;
            var outKey = Remap(kvp.Key);
            timing[outKey] = ToJsonNode(kvp.Value);
        }
        if (timing.Count > 0) root["timing"] = timing;

        var bus = new JsonObject();
        foreach (var kvp in p)
        {
            bool isBus = kvp.Key.StartsWith("bus.", StringComparison.Ordinal)
                         || BusFlatKeys.Contains(kvp.Key);
            if (!isBus) continue;
            var outKey = Remap(kvp.Key);
            bus[outKey] = ToJsonNode(kvp.Value);
        }
        if (bus.Count > 0) root["bus_config"] = bus;

        var reporting = new JsonObject();
        foreach (var kvp in p)
        {
            if (!kvp.Key.StartsWith("reporting.", StringComparison.Ordinal)) continue;
            var outKey = Remap(kvp.Key);
            reporting[outKey] = ToJsonNode(kvp.Value);
        }
        if (reporting.Count > 0) root["reporting"] = reporting;

        var execution = new JsonObject();
        foreach (var kvp in p)
        {
            if (!kvp.Key.StartsWith("execution.", StringComparison.Ordinal)) continue;
            var outKey = Remap(kvp.Key);
            execution[outKey] = ToJsonNode(kvp.Value);
        }
        if (execution.Count > 0) root["execution"] = execution;

        var advanced = new JsonObject();
        foreach (var kvp in p)
        {
            if (!kvp.Key.StartsWith("advanced.", StringComparison.Ordinal)) continue;
            var outKey = Remap(kvp.Key);
            advanced[outKey] = ToJsonNode(kvp.Value);
        }
        if (advanced.Count > 0) root["advanced"] = advanced;

        var safety = new JsonObject();
        foreach (var key in SafetyKeys)
        {
            if (!p.ContainsKey(key)) continue;
            var outKey = Remap(key);
            safety[outKey] = ToJsonNode(p[key]);
        }
        if (safety.Count > 0) root["safety"] = safety;

        if (p.TryGetValue("faults", out var faultsRaw))
        {
            var faultsArray = ToJsonArray(faultsRaw, remapObjectKeys: true);
            if (faultsArray is not null) root["faults"] = faultsArray;
        }

        if (p.TryGetValue("assertions", out var assertionsRaw))
        {
            var assertionsArray = ToJsonArray(assertionsRaw, remapObjectKeys: true);
            if (assertionsArray is not null) root["assertions"] = assertionsArray;
        }

        if (p.TryGetValue("signalList", out var signalListRaw))
        {
            var signalArray = ToJsonArray(signalListRaw, remapObjectKeys: true);
            if (signalArray is not null) root["signal_list"] = signalArray;
        }

        if (p.TryGetValue("sequenceScript", out var seqRaw))
        {
            var seqArray = ToJsonArray(seqRaw, remapObjectKeys: true);
            if (seqArray is not null) root["sequence_script"] = seqArray;
        }

        if (p.TryGetValue("sweepParameters", out var sweepRaw))
            root["sweep_parameters"] = ToJsonNode(sweepRaw);

        if (profile is not null)
            root["profile"] = BuildProfileNode(profile);

        var handledPrefixes = new[]
        {
            "initialConditions.", "timing.", "bus.", "reporting.",
            "execution.", "advanced."
        };
        var handledKeys = new HashSet<string>(StringComparer.Ordinal);
        handledKeys.UnionWith(TopLevelScalarKeys);
        handledKeys.UnionWith(SafetyKeys);
        handledKeys.UnionWith(TimingFlatKeys);
        handledKeys.UnionWith(BusFlatKeys);
        handledKeys.Add("faults");
        handledKeys.Add("assertions");
        handledKeys.Add("signalList");
        handledKeys.Add("sequenceScript");
        handledKeys.Add("sweepParameters");

        foreach (var kvp in p)
        {
            if (handledKeys.Contains(kvp.Key)) continue;
            if (handledPrefixes.Any(prefix => kvp.Key.StartsWith(prefix, StringComparison.Ordinal)))
                continue;

            if (System.Text.RegularExpressions.Regex.IsMatch(kvp.Key, @"^[A-Za-z_]\w*\[\d+\]"))
                continue;

            var dot = kvp.Key.IndexOf('.');
            if (dot > 0)
            {
                var section = CamelToSnake(kvp.Key[..dot]);
                var field   = Remap(kvp.Key[..dot] + "." + kvp.Key[(dot + 1)..],
                                    fallback: CamelToSnake(kvp.Key[(dot + 1)..]));
                if (root[section] is not JsonObject existing)
                {
                    existing = new JsonObject();
                    root[section] = existing;
                }
                existing[field] = ToJsonNode(kvp.Value);
            }
            else
            {
                var outKey = Remap(kvp.Key);
                root[outKey] = ToJsonNode(kvp.Value);
            }
        }

        return JsonSerializer.SerializeToDocument(root);
    }

    public JsonDocument FormatMany(
        IReadOnlyList<Scenario> scenarios,
        Profile? profile,
        GenerateRequest request)
    {
        var array = new JsonArray();
        foreach (var s in scenarios)
        {
            using var doc = FormatOne(s, profile, request);
            array.Add(doc.RootElement.Clone());
        }
        return JsonSerializer.SerializeToDocument(array);
    }

    private static Dictionary<string, object> NormalizeBracketKeys(
        Dictionary<string, object> p)
    {

        var bracketRx = new System.Text.RegularExpressions.Regex(
            @"^([A-Za-z_]\w*)\[(\d+)\](?:\.(.+))?$",
            System.Text.RegularExpressions.RegexOptions.Compiled);

        var groups = new Dictionary<string, SortedDictionary<int, Dictionary<string, object>>>(
            StringComparer.Ordinal);

        var result = new Dictionary<string, object>(StringComparer.Ordinal);
        var skipKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var key in p.Keys)
        {
            var m = bracketRx.Match(key);
            if (!m.Success) continue;
            skipKeys.Add(key);

            var baseName = m.Groups[1].Value;
            var idx      = int.Parse(m.Groups[2].Value);
            var subKey   = m.Groups[3].Success ? m.Groups[3].Value : null;

            if (!groups.TryGetValue(baseName, out var byIndex))
            {
                byIndex = new SortedDictionary<int, Dictionary<string, object>>();
                groups[baseName] = byIndex;
            }
            if (!byIndex.TryGetValue(idx, out var objDict))
            {
                objDict = new Dictionary<string, object>(StringComparer.Ordinal);
                byIndex[idx] = objDict;
            }

            if (subKey is not null)
                objDict[subKey] = p[key];
            else
            {

                var val = p[key];
                if (val is Dictionary<string, object> valDict)
                    foreach (var kvp in valDict) objDict[kvp.Key] = kvp.Value;
                else if (val is JsonElement je && je.ValueKind == JsonValueKind.Object)
                    foreach (var prop in je.EnumerateObject())
                        objDict[prop.Name] = prop.Value;
                else
                    objDict["_value"] = val;
            }
        }

        foreach (var kvp in p)
            if (!skipKeys.Contains(kvp.Key))
                result[kvp.Key] = kvp.Value;

        foreach (var (baseName, byIndex) in groups)
        {
            var list = new List<object>(byIndex.Count);
            foreach (var (_, objDict) in byIndex)
                list.Add((object)objDict);

            if (!result.ContainsKey(baseName))
                result[baseName] = list;

        }

        return result;
    }

    private static JsonObject BuildProfileNode(Profile profile)
    {
        var node = new JsonObject
        {
            ["profile_id"]  = profile.ProfileId,
            ["name"]        = profile.Name,
            ["description"] = profile.Description,
        };

        var assertions = new JsonArray();
        foreach (var a in profile.RecommendedAssertions) assertions.Add(a);
        node["recommended_assertions"] = assertions;

        return node;
    }

    private static string Remap(string key, string? fallback = null)
    {
        if (KeyMap.TryGetValue(key, out var mapped)) return mapped;
        return fallback ?? CamelToSnake(key);
    }

    public static string CamelToSnake(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new System.Text.StringBuilder(s.Length + 4);
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (char.IsUpper(c))
            {

                bool prevIsLower = i > 0 && char.IsLower(s[i - 1]);
                bool nextIsLower = i + 1 < s.Length && char.IsLower(s[i + 1]);
                bool prevIsUpper = i > 0 && char.IsUpper(s[i - 1]);
                if (i > 0 && (prevIsLower || (nextIsLower && prevIsUpper)))
                    sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    private static JsonNode? ToJsonNode(object? value)
    {
        if (value is null) return null;

        if (value is JsonElement je)
            return JsonNode.Parse(je.GetRawText());

        if (value is bool b)    return JsonValue.Create(b);
        if (value is int i)     return JsonValue.Create(i);
        if (value is long l)    return JsonValue.Create(l);
        if (value is double d)  return JsonValue.Create(d);
        if (value is float f)   return JsonValue.Create(f);
        if (value is decimal dc)return JsonValue.Create((double)dc);
        if (value is string str)return JsonValue.Create(str);

        if (value is System.Collections.IEnumerable enumerable and not string)
        {
            var arr = new JsonArray();
            foreach (var item in enumerable)
                arr.Add(ToJsonNode(item));
            return arr;
        }

        var json = JsonSerializer.Serialize(value);
        return JsonNode.Parse(json);
    }

    private static JsonArray? ToJsonArray(object? value, bool remapObjectKeys)
    {
        if (value is null) return null;

        if (value is JsonElement je)
        {
            if (je.ValueKind != JsonValueKind.Array) return null;

            var result = new JsonArray();
            foreach (var elem in je.EnumerateArray())
            {
                if (elem.ValueKind == JsonValueKind.Object && remapObjectKeys)
                    result.Add(RemapObjectNode(elem));
                else
                    result.Add(JsonNode.Parse(elem.GetRawText()));
            }
            return result;
        }

        if (value is not System.Collections.IEnumerable enumerable) return null;

        var arr = new JsonArray();
        foreach (var item in enumerable)
        {
            if (item is Dictionary<string, object> dict && remapObjectKeys)
                arr.Add(RemapDictNode(dict));
            else if (item is JsonElement elem2 && remapObjectKeys && elem2.ValueKind == JsonValueKind.Object)
                arr.Add(RemapObjectNode(elem2));
            else
                arr.Add(ToJsonNode(item));
        }
        return arr;
    }

    private static JsonObject RemapDictNode(Dictionary<string, object> dict)
    {
        var obj = new JsonObject();
        foreach (var kvp in dict)
            obj[Remap(kvp.Key)] = ToJsonNode(kvp.Value);
        return obj;
    }

    private static JsonObject RemapObjectNode(JsonElement elem)
    {
        var obj = new JsonObject();
        foreach (var prop in elem.EnumerateObject())
            obj[Remap(prop.Name)] = JsonNode.Parse(prop.Value.GetRawText());
        return obj;
    }
}
