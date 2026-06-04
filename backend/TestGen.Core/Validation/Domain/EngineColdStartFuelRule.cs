using TestGen.Core.Models;
using TestGen.Core.Services;

namespace TestGen.Core.Validation.Domain;

/// <summary>
/// Rule 18 / §6.3 (Engine ECU): an Engine-class ECU cannot run a cold-start
/// test with an empty fuel tank. Triggers when fuel_level_percent == 0 and
/// the scenario is a cold-start test (testType == "cold_start" or
/// profileId == "cold-start").
/// </summary>
public sealed class EngineColdStartFuelRule : IValidationRule
{
    public const string RuleCode = "ENGINE_COLD_START_EMPTY_FUEL";

    private readonly IEcuCatalogService _catalog;

    public EngineColdStartFuelRule(IEcuCatalogService catalog) => _catalog = catalog;

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var p = scenario.Parameters;

        if (!ParameterAccessor.TryGetString(p, "ecuId", out var ecuId)) yield break;

        var ecu = _catalog.GetById(ecuId);
        if (ecu is null ||
            !ecu.EcuClass.Equals("Engine", StringComparison.OrdinalIgnoreCase))
            yield break;

        if (!ParameterAccessor.TryGetDouble(
                p, "initialConditions.fuelLevelPercent", out var fuel) ||
            fuel > 0)
            yield break;

        if (!IsColdStartScenario(p, profile)) yield break;

        yield return new ValidationIssue
        {
            Severity = "error",
            Code     = RuleCode,
            Field    = "initialConditions.fuelLevelPercent",
            Message  = $"Engine ECU '{ecuId}': cold-start test cannot run with " +
                       "fuel_level_percent = 0."
        };
    }

    private static bool IsColdStartScenario(
        IDictionary<string, object> p, Profile profile)
    {
        if (ParameterAccessor.TryGetString(p, "testType", out var tt) &&
            tt.Equals("cold_start", StringComparison.OrdinalIgnoreCase))
            return true;

        return profile.ProfileId.Equals("cold-start", StringComparison.OrdinalIgnoreCase);
    }
}
