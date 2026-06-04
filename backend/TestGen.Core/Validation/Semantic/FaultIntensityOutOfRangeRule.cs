using System.Text.RegularExpressions;
using TestGen.Core.Models;

namespace TestGen.Core.Validation.Semantic;

public sealed class FaultIntensityOutOfRangeRule : IValidationRule
{
    public const string RuleCode = "FAULT_INTENSITY_OUT_OF_RANGE";

    private static readonly Regex FaultIndexPattern =
        new(@"^faults\[(\d+)\]\.", RegexOptions.Compiled);

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var p = scenario.Parameters;

        var indices = new HashSet<int>();
        foreach (var key in p.Keys)
        {
            var m = FaultIndexPattern.Match(key);
            if (m.Success && int.TryParse(m.Groups[1].Value, out var idx))
                indices.Add(idx);
        }

        foreach (var i in indices.OrderBy(static x => x))
        {
            if (!ParameterAccessor.TryGetDouble(p, $"faults[{i}].intensity", out var intensity))
                continue;

            if (intensity < 0.0 || intensity > 1.0)
            {
                ParameterAccessor.TryGetString(p, $"faults[{i}].faultId", out var faultId);
                yield return new ValidationIssue
                {
                    Severity = "error",
                    Code     = RuleCode,
                    Field    = $"faults[{i}].intensity",
                    Message  = $"Fault '{faultId}' intensity {intensity} is outside valid range [0.0 .. 1.0]."
                };
            }
        }
    }
}
