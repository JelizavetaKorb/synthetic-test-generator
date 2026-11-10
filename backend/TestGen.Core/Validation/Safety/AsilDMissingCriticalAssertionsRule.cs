using TestGen.Core.Models;

namespace TestGen.Core.Validation.Safety;

public sealed class AsilDMissingCriticalAssertionsRule : IValidationRule
{
    public const string RuleCode = "ASIL_D_MISSING_CRITICAL_ASSERTIONS";

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var p = scenario.Parameters;

        if (!ParameterAccessor.TryGetString(p, "asilLevel", out var asil)) yield break;
        if (!asil.Equals("D", StringComparison.OrdinalIgnoreCase))         yield break;

        if (!ParameterAccessor.AnyItemHas(p, "assertions", "severity", "critical"))
            yield return new ValidationIssue
            {
                Severity = "error",
                Code     = RuleCode,
                Field    = "asilLevel",
                Message  = "ASIL D scenarios require at least one assertion with severity 'critical'."
            };
    }
}
