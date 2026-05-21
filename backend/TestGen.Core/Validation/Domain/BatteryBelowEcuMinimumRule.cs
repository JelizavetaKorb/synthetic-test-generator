using TestGen.Core.Models;
using TestGen.Core.Services;

namespace TestGen.Core.Validation.Domain;

public sealed class BatteryBelowEcuMinimumRule : IValidationRule
{
    public const string RuleCode = "BATTERY_BELOW_ECU_MINIMUM";

    private readonly IEcuCatalogService _catalog;

    public BatteryBelowEcuMinimumRule(IEcuCatalogService catalog)
    {
        _catalog = catalog;
    }

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var p = scenario.Parameters;

        if (!ParameterAccessor.TryGetString(p, "ecuId", out var ecuId))
            yield break;

        if (!ParameterAccessor.TryGetDouble(p, "initialConditions.batteryVoltageV", out var voltage))
            yield break;

        var ecu = _catalog.GetById(ecuId);
        if (ecu is null)
            yield break;

        if (voltage < ecu.MinOperationalVoltage)
            yield return new ValidationIssue
            {
                Severity = "error",
                Code     = RuleCode,
                Field    = "initialConditions.batteryVoltageV",
                Message  = $"Battery voltage {voltage}V is below {ecu.EcuName} " +
                           $"minimum operational voltage {ecu.MinOperationalVoltage}V."
            };
    }
}
