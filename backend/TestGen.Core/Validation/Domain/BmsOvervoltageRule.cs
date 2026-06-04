using TestGen.Core.Models;
using TestGen.Core.Services;

namespace TestGen.Core.Validation.Domain;

/// <summary>
/// Rule 19 / §6.3 (BMS): for a BMS-class ECU, when battery_voltage_V exceeds
/// the catalog's max_operational_voltage the scenario must be explicitly
/// marked as an overvoltage safety test (testType == "safety_overvoltage_test"
/// or profileId starts with "safety-critical-asil-").
/// </summary>
public sealed class BmsOvervoltageRule : IValidationRule
{
    public const string RuleCode = "BMS_OVERVOLTAGE_WITHOUT_SAFETY_TEST";

    private readonly IEcuCatalogService _catalog;

    public BmsOvervoltageRule(IEcuCatalogService catalog) => _catalog = catalog;

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var p = scenario.Parameters;

        if (!ParameterAccessor.TryGetString(p, "ecuId", out var ecuId)) yield break;

        var ecu = _catalog.GetById(ecuId);
        if (ecu is null ||
            !ecu.EcuClass.Equals("BMS", StringComparison.OrdinalIgnoreCase))
            yield break;

        if (!ParameterAccessor.TryGetDouble(
                p, "initialConditions.batteryVoltageV", out var voltage))
            yield break;

        if (voltage <= ecu.MaxOperationalVoltage) yield break;

        if (IsSafetyOvervoltageScenario(p, profile)) yield break;

        yield return new ValidationIssue
        {
            Severity = "error",
            Code     = RuleCode,
            Field    = "initialConditions.batteryVoltageV",
            Message  = $"BMS '{ecuId}': battery voltage {voltage}V exceeds " +
                       $"max operational {ecu.MaxOperationalVoltage}V; this is " +
                       "only allowed inside a safety_overvoltage_test."
        };
    }

    private static bool IsSafetyOvervoltageScenario(
        IDictionary<string, object> p, Profile profile)
    {
        if (ParameterAccessor.TryGetString(p, "testType", out var tt) &&
            tt.Equals("safety_overvoltage_test", StringComparison.OrdinalIgnoreCase))
            return true;

        return profile.ProfileId.StartsWith(
            "safety-critical-asil-", StringComparison.OrdinalIgnoreCase);
    }
}
