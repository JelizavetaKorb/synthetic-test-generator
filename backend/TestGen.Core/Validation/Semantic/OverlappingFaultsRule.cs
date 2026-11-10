using TestGen.Core.Models;

namespace TestGen.Core.Validation.Semantic;

public sealed class OverlappingFaultsRule : IValidationRule
{
    public const string RuleCode = "FAULTS_OVERLAP";

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var faults = new List<(int Index, string Target, string Type, double Start, double End)>();
        foreach (var (i, f) in ParameterAccessor.GetItems(scenario.Parameters, "faults"))
            if (ParameterAccessor.TryGetString(f, "target",      out var target) &&
                ParameterAccessor.TryGetString(f, "type",        out var type)   &&
                ParameterAccessor.TryGetDouble(f, "startTimeMs", out var start)  &&
                ParameterAccessor.TryGetDouble(f, "durationMs",  out var duration))
                faults.Add((i, target, type, start, start + duration));

        for (var a = 0; a < faults.Count; a++)
        for (var b = a + 1; b < faults.Count; b++)
        {
            var x = faults[a];
            var y = faults[b];
            if (x.Target != y.Target) continue;
            if (x.Type.Equals(y.Type, StringComparison.OrdinalIgnoreCase)) continue;
            if (!(x.Start < y.End && y.Start < x.End)) continue;

            yield return new ValidationIssue
            {
                Severity = "error",
                Code     = RuleCode,
                Field    = $"faults[{y.Index}].startTimeMs",
                Message  = $"Faults {x.Index} ({x.Type}) and {y.Index} ({y.Type}) target '{x.Target}' " +
                           "with overlapping time windows. Merge or separate them."
            };
        }
    }
}
