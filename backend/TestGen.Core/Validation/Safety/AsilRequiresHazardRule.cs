using System.Text.RegularExpressions;
using TestGen.Core.Models;

namespace TestGen.Core.Validation.Safety;

public sealed class AsilRequiresHazardRule : IValidationRule
{
    public const string RuleCode = "ASIL_REQUIRES_HAZARD";

    private static readonly Regex SeverityPattern =
        new(@"^assertions\[\d+\]\.severity$", RegexOptions.Compiled);

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var p = scenario.Parameters;

        if (!ParameterAccessor.TryGetString(p, "asilLevel", out var asil) ||
            string.IsNullOrWhiteSpace(asil) ||
            asil.Equals("QM", StringComparison.OrdinalIgnoreCase))
            yield break;

        var hasHazard = ParameterAccessor.TryGetString(p, "hazardId", out var hazard) &&
                        !string.IsNullOrWhiteSpace(hazard);
        if (!hasHazard)
            yield return new ValidationIssue
            {
                Severity = "error",
                Code     = RuleCode,
                Field    = "hazardId",
                Message  = $"ASIL {asil} scenarios must reference a hazard_id."
            };

        var hasCritical = false;
        foreach (var key in p.Keys)
        {
            if (!SeverityPattern.IsMatch(key)) continue;
            if (ParameterAccessor.TryGetString(p, key, out var sev) &&
                sev.Equals("critical", StringComparison.OrdinalIgnoreCase))
            {
                hasCritical = true;
                break;
            }
        }

        if (asil.Equals("C", StringComparison.OrdinalIgnoreCase) && !hasCritical)
            yield return new ValidationIssue
            {
                Severity = "error",
                Code     = RuleCode,
                Field    = "asilLevel",
                Message  = "ASIL C scenarios require at least one assertion with severity 'critical'."
            };
    }
}
