using TestGen.Core.Models;

namespace TestGen.Core.Validation.Semantic;

public sealed class CalibrationIgnitionOffRule : IValidationRule
{
    public const string RuleCode = "CALIBRATION_REQUIRES_IGNITION";

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var p = scenario.Parameters;

        if (!ParameterAccessor.TryGetString(p, "ecuMode", out var ecuMode))                             yield break;
        if (!ParameterAccessor.TryGetString(p, "initialConditions.ignitionKey", out var ignitionKey))  yield break;

        if (!ecuMode.Equals("CALIBRATION", StringComparison.OrdinalIgnoreCase)) yield break;
        if (!ignitionKey.Equals("OFF", StringComparison.OrdinalIgnoreCase))     yield break;

        ParameterAccessor.TryGetString(p, "targetExecution", out var targetExecution);

        if (targetExecution.Equals("bench", StringComparison.OrdinalIgnoreCase))
            yield return new ValidationIssue
            {
                Severity = "warning",
                Code     = RuleCode,
                Field    = "ecuMode",
                Message  = "Calibration mode with ignition OFF is only valid in bench testing."
            };
        else
            yield return new ValidationIssue
            {
                Severity = "error",
                Code     = RuleCode,
                Field    = "ecuMode",
                Message  = "Calibration mode requires ignition to be ON."
            };
    }
}

