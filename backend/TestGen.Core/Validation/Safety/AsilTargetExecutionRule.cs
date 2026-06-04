using TestGen.Core.Models;

namespace TestGen.Core.Validation.Safety;

public sealed class AsilTargetExecutionRule : IValidationRule
{
    public const string RuleCode = "ASIL_CD_SIMULATION_ONLY";

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var p = scenario.Parameters;

        if (!ParameterAccessor.TryGetString(p, "asilLevel", out var asilLevel))         yield break;
        if (!ParameterAccessor.TryGetString(p, "targetExecution", out var targetExec))  yield break;

        var isAsilCD = asilLevel.Equals("C", StringComparison.OrdinalIgnoreCase) ||
                       asilLevel.Equals("D", StringComparison.OrdinalIgnoreCase);

        if (isAsilCD && targetExec.Equals("simulation", StringComparison.OrdinalIgnoreCase))
            yield return new ValidationIssue
            {
                Severity = "warning",
                Code     = RuleCode,
                Field    = "targetExecution",
                Message  = "ASIL C/D scenarios should use HIL or lab execution, not simulation only."
            };
    }
}
