using TestGen.Core.Models;

namespace TestGen.Core.Validation.Safety;

public sealed class AsilDFormalAssertionRule : IValidationRule
{
    public const string RuleCode = "ASIL_D_MISSING_FORMAL_ASSERTION";

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var p = scenario.Parameters;

        if (!ParameterAccessor.TryGetString(p, "asilLevel", out var asil)) yield break;
        if (!asil.Equals("D", StringComparison.OrdinalIgnoreCase))         yield break;

        var hasFormal = ParameterAccessor.GetItems(p, "assertions").Any(static a =>
            ParameterAccessor.TryGetString(a.Fields, "type", out var type) &&
            (type.Equals("STL", StringComparison.OrdinalIgnoreCase) ||
             type.Equals("LTL", StringComparison.OrdinalIgnoreCase)));

        if (!hasFormal)
            yield return new ValidationIssue
            {
                Severity = "error",
                Code     = RuleCode,
                Field    = "asilLevel",
                Message  = "ASIL D scenarios require at least one formal (STL or LTL) assertion."
            };
    }
}
