using TestGen.Core.Models;

namespace TestGen.Core.Validation.Semantic;

public sealed class HilTargetRunnerRule : IValidationRule
{
    public const string RuleCode = "HIL_REQUIRES_HIL_BENCH";

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var p = scenario.Parameters;

        if (!ParameterAccessor.TryGetString(p, "targetExecution", out var exec)) yield break;
        if (!exec.Equals("HIL", StringComparison.OrdinalIgnoreCase))             yield break;

        if (!ParameterAccessor.TryGetString(p, "execution.targetRunner", out var runner) ||
            string.IsNullOrWhiteSpace(runner))
        {
            yield return new ValidationIssue
            {
                Severity = "error",
                Code     = RuleCode,
                Field    = "execution.targetRunner",
                Message  = "target_execution is HIL but no execution.target_runner is configured. " +
                           "Set execution.target_runner to 'HIL_bench'."
            };
            yield break;
        }

        if (!runner.Equals("HIL_bench", StringComparison.OrdinalIgnoreCase))
            yield return new ValidationIssue
            {
                Severity = "error",
                Code     = RuleCode,
                Field    = "execution.targetRunner",
                Message  = $"target_execution is HIL but execution.target_runner is '{runner}'. " +
                           "It must be 'HIL_bench'."
            };
    }
}
