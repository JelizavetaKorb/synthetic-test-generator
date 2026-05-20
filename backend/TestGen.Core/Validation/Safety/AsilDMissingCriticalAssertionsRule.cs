using System.Text.RegularExpressions;
using TestGen.Core.Models;

namespace TestGen.Core.Validation.Safety;

public sealed class AsilDMissingCriticalAssertionsRule : IValidationRule
{
    public const string RuleCode = "ASIL_D_MISSING_CRITICAL_ASSERTIONS";

    private static readonly Regex AssertionSeverityPattern =
        new(@"^assertions\[\d+\]\.severity$", RegexOptions.Compiled);

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var p = scenario.Parameters;

        if (!ParameterAccessor.TryGetString(p, "asilLevel", out var asil)) yield break;
        if (!asil.Equals("D", StringComparison.OrdinalIgnoreCase))         yield break;

        var hasCritical = false;
        foreach (var key in p.Keys)
        {
            if (!AssertionSeverityPattern.IsMatch(key)) continue;
            if (ParameterAccessor.TryGetString(p, key, out var sev) &&
                sev.Equals("critical", StringComparison.OrdinalIgnoreCase))
            {
                hasCritical = true;
                break;
            }
        }

        if (!hasCritical)
            yield return new ValidationIssue
            {
                Severity = "error",
                Code     = RuleCode,
                Field    = "asilLevel",
                Message  = "ASIL D scenarios require at least one assertion with severity 'critical'."
            };
    }
}
