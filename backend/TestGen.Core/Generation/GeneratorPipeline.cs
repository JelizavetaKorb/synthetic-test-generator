using TestGen.Core.Models;
using TestGen.Core.Services;
using TestGen.Core.Validation;

namespace TestGen.Core.Generation;

public sealed class PrepareInputsResult
{
    public Profile?                                    Profile        { get; init; }
    public Dictionary<string, object>                 BaseParameters { get; init; } = new();
    public IReadOnlyList<(string Key, object[] Values)> Sweeps       { get; init; } = Array.Empty<(string, object[])>();
    public IReadOnlyList<ValidationIssue>              Issues        { get; init; } = Array.Empty<ValidationIssue>();

    public bool HasErrors => Issues.Count > 0;
}

public sealed class GeneratorPipeline
{
    private readonly IProfileService     _profileService;
    private readonly IScenarioValidator  _validator;

    private static readonly Profile NoProfile = new()
    {
        ProfileId             = string.Empty,
        Name                  = "scenario",
        DefaultParameters     = new Dictionary<string, object>(),
        LockedParameters      = Array.Empty<string>(),
        RecommendedAssertions = Array.Empty<string>()
    };

    public GeneratorPipeline(IProfileService profileService, IScenarioValidator validator)
    {
        _profileService = profileService;
        _validator      = validator;
    }

    public PrepareInputsResult PrepareInputs(GenerateRequest request)
    {
        Profile profile;
        var baseParameters = new Dictionary<string, object>();

        if (string.IsNullOrWhiteSpace(request.ProfileId))
        {
            profile = NoProfile;
        }
        else
        {
            var found = _profileService.GetById(request.ProfileId);
            if (found is null)
                return new PrepareInputsResult
                {
                    Issues = new[]
                    {
                        new ValidationIssue
                        {
                            Severity = "error",
                            Code     = "PROFILE_NOT_FOUND",
                            Field    = "profileId",
                            Message  = $"Profile '{request.ProfileId}' not found."
                        }
                    }
                };
            profile = found;
            baseParameters = new Dictionary<string, object>(profile.DefaultParameters);
        }

        if (request.Overrides is not null)
            foreach (var (k, v) in request.Overrides)
                baseParameters[k] = v;

        var sweeps = new List<(string Key, object[] Values)>();
        var issues = new List<ValidationIssue>();

        if (request.SweepParameters is not null)
        {
            foreach (var (key, spec) in request.SweepParameters)
            {
                var (values, issue) = ExpandSweep(key, spec);
                if (issue is not null) issues.Add(issue);
                if (values is not null) sweeps.Add((key, values));
            }
        }

        if (issues.Count > 0)
            return new PrepareInputsResult { Profile = profile, Issues = issues };

        sweeps.Sort(static (a, b) => string.Compare(a.Key, b.Key, StringComparison.Ordinal));

        return new PrepareInputsResult
        {
            Profile        = profile,
            BaseParameters = baseParameters,
            Sweeps         = sweeps
        };
    }

    private static (object[]? Values, ValidationIssue? Issue) ExpandSweep(
        string key, SweepSpec spec) =>
        spec.Type.ToLowerInvariant() switch
        {
            "fixed"           => ExpandFixed(key, spec),
            "linear_range"    => ExpandLinearRange(key, spec),
            "geometric_range" => ExpandGeometricRange(key, spec),
            "enum_list"       => ExpandEnumList(key, spec),
            "random_uniform"  => ExpandRandomUniform(key, spec),
            "gaussian"        => ExpandGaussian(key, spec),
            "latin_hypercube" => ExpandLatinHypercube(key, spec),
            _                 => (null, SweepError(key, "UNSUPPORTED_SWEEP_TYPE",
                $"Sweep type '{spec.Type}' is not supported. Supported types: " +
                "fixed, linear_range, geometric_range, enum_list, random_uniform, gaussian, latin_hypercube."))
        };

    private static (object[]?, ValidationIssue?) ExpandFixed(string key, SweepSpec spec)
    {
        if (spec.Values is { Length: > 0 }) return (new[] { spec.Values[0] }, null);
        if (spec.From is not null)          return (new object[] { spec.From.Value }, null);
        return (null, SweepError(key, "INVALID_SWEEP_SPEC",
            $"Sweep '{key}': fixed requires 'values' or 'from'."));
    }

    private static (object[]?, ValidationIssue?) ExpandLinearRange(string key, SweepSpec spec)
    {
        if (spec.From is null || spec.To is null || spec.Step is null)
            return (null, SweepError(key, "SWEEP_MISSING_RANGE",
                $"Sweep '{key}': linear_range requires 'from', 'to', and 'step'."));
        return (LinearRange(spec.From.Value, spec.To.Value, spec.Step.Value), null);
    }

    private static (object[]?, ValidationIssue?) ExpandGeometricRange(string key, SweepSpec spec)
    {
        if (spec.From is null || spec.To is null || spec.Step is null)
            return (null, SweepError(key, "INVALID_SWEEP_SPEC",
                $"Sweep '{key}': geometric_range requires 'from', 'to', and 'step' (ratio)."));
        if (spec.From.Value <= 0 || spec.Step.Value <= 1)
            return (null, SweepError(key, "INVALID_SWEEP_SPEC",
                $"Sweep '{key}': geometric_range requires 'from' > 0 and 'step' > 1."));
        return (GeometricRange(spec.From.Value, spec.To.Value, spec.Step.Value), null);
    }

    private static (object[]?, ValidationIssue?) ExpandEnumList(string key, SweepSpec spec)
    {
        if (spec.Values is null || spec.Values.Length == 0)
            return (null, SweepError(key, "INVALID_SWEEP_SPEC",
                $"Sweep '{key}': enum_list requires a non-empty 'values' array."));

        var coerced = spec.Values.Select(CoerceStringToNumber).ToArray();
        return (coerced, null);
    }

    private static object CoerceStringToNumber(object v)
    {
        string? s = v switch
        {
            string str => str,
            System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.String } je
                => je.GetString(),
            _ => null
        };
        if (s is null) return v;
        if (long.TryParse(s, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var l)) return l;
        if (double.TryParse(s, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var d)) return d;
        return v;
    }

    public const int DefaultSampleCount = 5;
    public const int SampleSeed         = 42;

    private static (object[]?, ValidationIssue?) ExpandRandomUniform(string key, SweepSpec spec)
    {
        if (spec.From is null || spec.To is null)
            return (null, SweepError(key, "INVALID_SWEEP_SPEC",
                $"Sweep '{key}': random_uniform requires 'from' and 'to'."));

        var from  = spec.From.Value;
        var range = spec.To.Value - from;
        return Sample(key, spec, (random, _, _) => from + random.NextDouble() * range);
    }

    private static (object[]?, ValidationIssue?) ExpandGaussian(string key, SweepSpec spec)
    {
        if (spec.Mean is null || spec.Std is null)
            return (null, SweepError(key, "INVALID_SWEEP_SPEC",
                $"Sweep '{key}': gaussian requires 'mean' and 'std'."));
        if (spec.Std.Value < 0)
            return (null, SweepError(key, "INVALID_SWEEP_SPEC",
                $"Sweep '{key}': gaussian requires 'std' >= 0."));

        var mean = spec.Mean.Value;
        var std  = spec.Std.Value;
        return Sample(key, spec, (random, _, _) =>
            mean + std * Math.Sqrt(-2.0 * Math.Log(1.0 - random.NextDouble()))
                       * Math.Cos(2.0 * Math.PI * random.NextDouble()));
    }

    private static (object[]?, ValidationIssue?) ExpandLatinHypercube(string key, SweepSpec spec)
    {
        if (spec.From is null || spec.To is null)
            return (null, SweepError(key, "INVALID_SWEEP_SPEC",
                $"Sweep '{key}': latin_hypercube requires 'from' and 'to'."));

        var from  = spec.From.Value;
        var range = spec.To.Value - from;
        return Sample(key, spec, (random, i, count) => from + (i + random.NextDouble()) * range / count);
    }

    private static (object[]?, ValidationIssue?) Sample(
        string key, SweepSpec spec, Func<Random, int, int, double> draw)
    {
        var count = spec.Count ?? DefaultSampleCount;
        if (count < 1)
            return (null, SweepError(key, "INVALID_SWEEP_SPEC",
                $"Sweep '{key}': {spec.Type} requires 'count' >= 1."));

        var random = new Random(SampleSeed);
        var values = new object[count];
        for (var i = 0; i < count; i++)
            values[i] = Math.Round(draw(random, i, count), 6);
        return (values, null);
    }

    private static ValidationIssue SweepError(string key, string code, string message) =>
        new() { Severity = "error", Code = code, Field = key, Message = message };

    private static readonly Dictionary<string, string> SweepKeyRoutes =
        new(StringComparer.OrdinalIgnoreCase)
        {

            ["battery_voltage_V"]      = "initialConditions.batteryVoltageV",
            ["ambient_temperature_C"]  = "initialConditions.ambientTemperatureC",
            ["engine_state"]           = "initialConditions.engineState",
            ["fuel_level_percent"]     = "initialConditions.fuelLevelPercent",
            ["vehicle_speed_mps"]      = "initialConditions.vehicleSpeedMps",
            ["gear_position"]          = "initialConditions.gearPosition",
            ["ignition_key"]           = "initialConditions.ignitionKey",
            ["ambient_humidity_pct"]   = "initialConditions.ambientHumidityPct",
            ["gps_fix_quality"]        = "initialConditions.gpsFixQuality",
            ["localization_error_m"]   = "initialConditions.localizationErrorM",

            ["test_duration_ms"]        = "testDurationMs",
            ["time_resolution_ms"]      = "timeResolutionMs",
            ["startup_timeout_ms"]      = "timing.startupTimeoutMs",
            ["warmup_duration_s"]       = "timing.warmupDurationS",
            ["measurement_interval_ms"] = "timing.measurementIntervalMs",
            ["repetitions"]             = "timing.repetitions",
            ["inter_test_delay_ms"]     = "timing.interTestDelayMs",

            ["bus_load_pct"]            = "bus.busLoadPct",
            ["message_jitter_ms"]       = "bus.messageJitterMs",
            ["bitrate_kbps"]            = "bus.bitrateKbps",

            ["telemetry_frequency_hz"]  = "reporting.telemetryFrequencyHz",
            ["log_level"]               = "reporting.logLevel",

            ["EMI_level"]                 = "environmental_perturbations.EMI_level",
            ["temperature_ramp.start_C"]  = "environmental_perturbations.temperature_ramp.start_C",
            ["temperature_ramp.end_C"]    = "environmental_perturbations.temperature_ramp.end_C",
            ["temperature_ramp.rate_C_per_min"]
                                           = "environmental_perturbations.temperature_ramp.rate_C_per_min",
        };

    private static string RouteSweepKey(string key)
    {
        if (key.Contains('.')) return key;
        return SweepKeyRoutes.TryGetValue(key, out var routed) ? routed : key;
    }

    public GenerateResult MaterializeAndFilter(
        Profile                                            profile,
        Dictionary<string, object>                        baseParameters,
        IEnumerable<IReadOnlyDictionary<string, object>>  combinations)
    {
        var scenarios = new List<Scenario>();
        var allIssues = new List<ValidationIssue>();
        var index     = 0;

        foreach (var combination in combinations)
        {
            index++;
            var parameters = new Dictionary<string, object>(baseParameters);
            foreach (var (k, v) in combination)
                parameters[RouteSweepKey(k)] = v;

            var candidate = new Scenario
            {
                ScenarioId  = Guid.NewGuid().ToString(),
                Name        = string.IsNullOrEmpty(profile.ProfileId) ? $"scenario-{index:D3}" : $"{profile.Name} #{index:D3}",
                Description = string.IsNullOrEmpty(profile.ProfileId) ? "Generated without a profile." : $"Generated from profile '{profile.ProfileId}'.",
                Parameters  = parameters
            };

            var vr     = _validator.Validate(candidate, profile);
            var prefix = $"[scenario {index:D3}] ";

            foreach (var issue in vr.Issues)
                allIssues.Add(issue with { Message = prefix + issue.Message });

            if (vr.IsValid)
                scenarios.Add(candidate);
        }

        return new GenerateResult { Scenarios = scenarios, Issues = allIssues };
    }

    public static object[] LinearRange(double from, double to, double step)
    {
        var values = new List<object>();
        var v      = from;
        while (v <= to + 1e-9)
        {
            values.Add(Math.Round(v, 6));
            v += step;
        }
        return values.ToArray();
    }

    public static object[] GeometricRange(double from, double to, double step)
    {
        var values = new List<object>();
        var v      = from;
        while (v <= to + 1e-9)
        {
            values.Add(Math.Round(v, 6));
            v *= step;
        }
        return values.ToArray();
    }
}
