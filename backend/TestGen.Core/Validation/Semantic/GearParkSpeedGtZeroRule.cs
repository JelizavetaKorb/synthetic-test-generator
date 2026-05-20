using TestGen.Core.Models;

namespace TestGen.Core.Validation.Semantic;

public sealed class GearParkSpeedGtZeroRule : IValidationRule
{
    public const string RuleCode = "GEAR_PARK_SPEED_GT_ZERO";

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var p = scenario.Parameters;

        if (!ParameterAccessor.TryGetString(p, "initialConditions.gearPosition",    out var gear))  yield break;
        if (!ParameterAccessor.TryGetDouble(p, "initialConditions.vehicleSpeedMps", out var speed)) yield break;

        if (gear.Equals("P", StringComparison.OrdinalIgnoreCase) && speed > 0)
            yield return new ValidationIssue
            {
                Severity = "error",
                Code     = RuleCode,
                Field    = "initialConditions.vehicleSpeedMps",
                Message  = $"Gear is in Park but vehicle speed is {speed} m/s. " +
                           "Park position requires zero speed."
            };
    }
}
