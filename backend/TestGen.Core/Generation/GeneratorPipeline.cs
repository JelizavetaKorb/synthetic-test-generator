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

    public GeneratorPipeline(IProfileService profileService, IScenarioValidator validator)
    {
        _profileService = profileService;
        _validator      = validator;
    }

    public PrepareInputsResult PrepareInputs(GenerateRequest request)
    {
        var profile = _profileService.GetById(request.ProfileId);
        if (profile is null)
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

        var baseParameters = new Dictionary<string, object>(profile.DefaultParameters);
        if (request.Overrides is not null)
            foreach (var (k, v) in request.Overrides)
                baseParameters[k] = v;

        var sweeps = new List<(string Key, object[] Values)>();
        var issues = new List<ValidationIssue>();

        if (request.SweepParameters is not null)
        {
            foreach (var (key, spec) in request.SweepParameters)
            {
                if (spec.Type.Equals("linear_range", StringComparison.OrdinalIgnoreCase))
                {
                    if (spec.From is null || spec.To is null || spec.Step is null)
                    {
                        issues.Add(new ValidationIssue
                        {
                            Severity = "error",
                            Code     = "SWEEP_MISSING_RANGE",
                            Field    = key,
                            Message  = $"Sweep '{key}': linear_range requires 'from', 'to', and 'step'."
                        });
                        continue;
                    }
                    sweeps.Add((key, LinearRange(spec.From.Value, spec.To.Value, spec.Step.Value)));
                }
                else if (spec.Type.Equals("fixed", StringComparison.OrdinalIgnoreCase))
                {
                    if (spec.Values is { Length: > 0 })
                        sweeps.Add((key, new object[] { spec.Values[0] }));
                    else if (spec.From is not null)
                        sweeps.Add((key, new object[] { spec.From.Value }));
                    else
                        issues.Add(new ValidationIssue { Severity = "error", Code = "INVALID_SWEEP_SPEC", Field = key, Message = $"Sweep '{key}': fixed requires 'values' or 'from'." });
                }
                else if (spec.Type.Equals("enum_list", StringComparison.OrdinalIgnoreCase))
                {
                    if (spec.Values is null || spec.Values.Length == 0)
                    {
                        issues.Add(new ValidationIssue { Severity = "error", Code = "INVALID_SWEEP_SPEC", Field = key, Message = $"Sweep '{key}': enum_list requires a non-empty 'values' array." });
                        continue;
                    }
                    sweeps.Add((key, spec.Values));
                }
                else if (spec.Type.Equals("random_uniform", StringComparison.OrdinalIgnoreCase))
                {
                    if (spec.From is null || spec.To is null)
                    {
                        issues.Add(new ValidationIssue { Severity = "error", Code = "INVALID_SWEEP_SPEC", Field = key, Message = $"Sweep '{key}': random_uniform requires 'from' and 'to'." });
                        continue;
                    }
                    var count  = spec.Count ?? 5;
                    var random = new Random(42);
                    var vals   = new object[count];
                    for (var i = 0; i < count; i++)
                        vals[i] = Math.Round(spec.From.Value + random.NextDouble() * (spec.To.Value - spec.From.Value), 6);
                    sweeps.Add((key, vals));
                }
                else if (spec.Type.Equals("geometric_range", StringComparison.OrdinalIgnoreCase))
                {
                    if (spec.From is null || spec.To is null || spec.Step is null)
                    {
                        issues.Add(new ValidationIssue { Severity = "error", Code = "INVALID_SWEEP_SPEC", Field = key, Message = $"Sweep '{key}': geometric_range requires 'from', 'to', and 'step' (ratio)." });
                        continue;
                    }
                    if (spec.From.Value <= 0 || spec.Step.Value <= 1)
                    {
                        issues.Add(new ValidationIssue { Severity = "error", Code = "INVALID_SWEEP_SPEC", Field = key, Message = $"Sweep '{key}': geometric_range requires 'from' > 0 and 'step' > 1." });
                        continue;
                    }
                    sweeps.Add((key, GeometricRange(spec.From.Value, spec.To.Value, spec.Step.Value)));
                }
                else
                {
                    issues.Add(new ValidationIssue
                    {
                        Severity = "error",
                        Code     = "UNSUPPORTED_SWEEP_TYPE",
                        Field    = key,
                        Message  = $"Sweep type '{spec.Type}' is not supported. Supported types: fixed, linear_range, geometric_range, enum_list, random_uniform."
                    });
                }
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
                parameters[k] = v;

            var candidate = new Scenario
            {
                ScenarioId  = Guid.NewGuid().ToString(),
                Name        = $"{profile.Name} #{index:D3}",
                Description = $"Generated from profile '{profile.ProfileId}'.",
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
