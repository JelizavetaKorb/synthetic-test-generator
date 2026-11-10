using TestGen.Core.Models;

namespace TestGen.Core.Validation.Semantic;

public sealed class FaultIntensityOutOfRangeRule : IValidationRule
{
    public const string RuleCode = "FAULT_INTENSITY_OUT_OF_RANGE";

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        foreach (var (i, fault) in ParameterAccessor.GetItems(scenario.Parameters, "faults"))
        {
            if (!ParameterAccessor.TryGetDouble(fault, "intensity", out var intensity)) continue;
            if (intensity >= 0.0 && intensity <= 1.0) continue;

            ParameterAccessor.TryGetString(fault, "faultId", out var faultId);
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
