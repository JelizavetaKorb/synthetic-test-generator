using TestGen.Core.Models;

namespace TestGen.Core.Validation.Syntactic;

public sealed class LockedParameterOverriddenRule : IValidationRule
{
    public const string RuleCode = "LOCKED_PARAMETER_OVERRIDDEN";

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        foreach (var key in profile.LockedParameters)
        {
            if (!ParameterAccessor.TryGetString(profile.DefaultParameters, key, out var defaultVal))
                continue;

            if (!ParameterAccessor.TryGetString(scenario.Parameters, key, out var scenarioVal))
                continue;

            if (!string.Equals(defaultVal, scenarioVal, StringComparison.OrdinalIgnoreCase))
                yield return new ValidationIssue
                {
                    Severity = "error",
                    Code     = RuleCode,
                    Field    = key,
                    Message  = $"Parameter '{key}' is locked by profile '{profile.Name}' " +
                               "and cannot be overridden."
                };
        }
    }
}
