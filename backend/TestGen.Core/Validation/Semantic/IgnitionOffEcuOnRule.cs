using TestGen.Core.Models;

namespace TestGen.Core.Validation.Semantic;

public sealed class IgnitionOffEcuOnRule : IValidationRule
{
    public const string RuleCode = "IGNITION_OFF_ECU_ON";

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var p = scenario.Parameters;

        if (!ParameterAccessor.TryGetString(p, "initialConditions.ignitionKey", out var ignition)) yield break;
        if (!ParameterAccessor.TryGetString(p, "ecuPowerState",                 out var power))    yield break;

        if (ignition.Equals("OFF", StringComparison.OrdinalIgnoreCase) &&
            power.Equals("ON",     StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationIssue
            {
                Severity = "warning",
                Code     = RuleCode,
                Field    = "ecuPowerState",
                Message  = "Ignition is OFF but ECU power state is ON. " +
                           "This is only valid for bench testing."
            };
        }
    }
}
