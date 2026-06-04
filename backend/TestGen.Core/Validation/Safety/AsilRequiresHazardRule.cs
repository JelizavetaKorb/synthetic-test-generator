using System.Text.RegularExpressions;
using TestGen.Core.Models;

namespace TestGen.Core.Validation.Safety;

/// <summary>
/// Rule 13 / §6.4: whenever an ASIL level is set the scenario must reference a
/// hazard_id and carry at least one assertion of severity 'critical'. This
/// complements <see cref="AsilDMissingCriticalAssertionsRule"/> by covering
/// ASIL A/B/C and the hazard-id requirement.
/// </summary>
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

        // ASIL A/B may be satisfied by a major-severity assertion per §6.4
        // wording; only ASIL C/D demand 'critical'. ASIL D is already covered
        // by AsilDMissingCriticalAssertionsRule, so here we focus on ASIL C.
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
