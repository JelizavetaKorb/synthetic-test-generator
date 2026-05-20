using TestGen.Core.Models;

namespace TestGen.Core.Validation.Semantic;

public sealed class EngineOffSpeedGtZeroRule : IValidationRule
{
    public const string RuleCode = "ENGINE_OFF_SPEED_GT_ZERO";

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var p = scenario.Parameters;

        if (!ParameterAccessor.TryGetString(p, "initialConditions.engineState",     out var state)) yield break;
        if (!ParameterAccessor.TryGetDouble(p, "initialConditions.vehicleSpeedMps", out var speed)) yield break;

        if (state.Equals("OFF", StringComparison.OrdinalIgnoreCase) && speed > 0)
            yield return new ValidationIssue
            {
                Severity = "error",
                Code     = RuleCode,
                Field    = "initialConditions.vehicleSpeedMps",
                Message  = $"Engine is OFF but vehicle speed is {speed} m/s. " +
                           "This combination is physically impossible."
            };
    }
}
