using TestGen.Core.Models;
using TestGen.Core.Services;

namespace TestGen.Core.Validation.Domain;

public sealed class BrakeMissingWheelSpeedRule : IValidationRule
{
    public const string RuleCode = "BRAKE_WHEEL_SPEED_MISSING";

    private const string WheelSpeedToken   = "wheel_speed";
    private const string WheelSpeedMessage = "MSG_WHEEL_SPEED";

    private readonly IEcuCatalogService _catalog;

    public BrakeMissingWheelSpeedRule(IEcuCatalogService catalog) => _catalog = catalog;

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var p = scenario.Parameters;

        if (!ParameterAccessor.TryGetString(p, "ecuId", out var ecuId)) yield break;

        var ecu = _catalog.GetById(ecuId);
        if (ecu is null ||
            !ecu.EcuClass.Equals("Brake", StringComparison.OrdinalIgnoreCase))
            yield break;

        if (!AnyAssertionReferencesWheelSpeed(p)) yield break;

        if (BusOrSignalsDeclareWheelSpeed(p)) yield break;

        yield return new ValidationIssue
        {
            Severity = "error",
            Code     = RuleCode,
            Field    = "bus.messageDefs",
            Message  = $"Brake ECU '{ecuId}': assertions reference 'wheel_speed' " +
                       $"but neither '{WheelSpeedMessage}' nor a 'wheel_speed' " +
                       "signal is declared in bus.messageDefs / signal_list."
        };
    }

    private static bool AnyAssertionReferencesWheelSpeed(IDictionary<string, object> p) =>
        ParameterAccessor.GetItems(p, "assertions").Any(static a =>
            ParameterAccessor.TryGetString(a.Fields, "expr", out var expr) &&
            expr.Contains(WheelSpeedToken, StringComparison.OrdinalIgnoreCase));

    private static bool BusOrSignalsDeclareWheelSpeed(IDictionary<string, object> p) =>
        ParameterAccessor.GetItems(p, "messageDefs").Any(static m =>
            IsWheelSpeed(m.Fields, "id", allowMessageName: true) ||
            IsWheelSpeed(m.Fields, "name", allowMessageName: true)) ||
        ParameterAccessor.GetItems(p, "signalList").Any(static s =>
            IsWheelSpeed(s.Fields, "name", allowMessageName: false));

    private static bool IsWheelSpeed(IDictionary<string, object> item, string key, bool allowMessageName) =>
        ParameterAccessor.TryGetString(item, key, out var value) &&
        ((allowMessageName && value.Equals(WheelSpeedMessage, StringComparison.OrdinalIgnoreCase)) ||
         value.Contains(WheelSpeedToken, StringComparison.OrdinalIgnoreCase));
}
