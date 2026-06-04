using System.Text.Json;
using System.Text.RegularExpressions;
using TestGen.Core.Models;
using TestGen.Core.Services;

namespace TestGen.Core.Validation.Domain;

public sealed class BrakeMissingWheelSpeedRule : IValidationRule
{
    public const string RuleCode = "BRAKE_WHEEL_SPEED_MISSING";

    private const string WheelSpeedToken   = "wheel_speed";
    private const string WheelSpeedMessage = "MSG_WHEEL_SPEED";

    private static readonly Regex AssertionExprPattern =
        new(@"^assertions\[\d+\]\.expr$", RegexOptions.Compiled);

    private static readonly Regex MessageIdNamePattern =
        new(@"^messageDefs\[\d+\]\.(id|name)$", RegexOptions.Compiled);

    private static readonly Regex SignalNamePattern =
        new(@"^signalList\[\d+\]\.name$", RegexOptions.Compiled);

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

    private static bool AnyAssertionReferencesWheelSpeed(IDictionary<string, object> p)
    {
        foreach (var key in p.Keys)
        {
            if (!AssertionExprPattern.IsMatch(key)) continue;
            if (ParameterAccessor.TryGetString(p, key, out var expr) &&
                expr.Contains(WheelSpeedToken, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static bool BusOrSignalsDeclareWheelSpeed(IDictionary<string, object> p)
    {
        foreach (var key in p.Keys)
        {
            if (MessageIdNamePattern.IsMatch(key) &&
                ParameterAccessor.TryGetString(p, key, out var v) &&
                (v.Equals(WheelSpeedMessage, StringComparison.OrdinalIgnoreCase) ||
                 v.Contains(WheelSpeedToken, StringComparison.OrdinalIgnoreCase)))
                return true;

            if (SignalNamePattern.IsMatch(key) &&
                ParameterAccessor.TryGetString(p, key, out var s) &&
                s.Contains(WheelSpeedToken, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        if (p.TryGetValue("messageDefs", out var msgRaw) && msgRaw is JsonElement msgEl &&
            msgEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in msgEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;
                if (HasMatchingProp(item, "id")   ||
                    HasMatchingProp(item, "name")) return true;
            }
        }

        if (p.TryGetValue("signalList", out var sigRaw) && sigRaw is JsonElement sigEl &&
            sigEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in sigEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;
                if (item.TryGetProperty("name", out var n) &&
                    n.ValueKind == JsonValueKind.String &&
                    n.GetString()!.Contains(WheelSpeedToken, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;

        static bool HasMatchingProp(JsonElement item, string prop) =>
            item.TryGetProperty(prop, out var el) &&
            el.ValueKind == JsonValueKind.String &&
            (el.GetString()!.Equals(WheelSpeedMessage, StringComparison.OrdinalIgnoreCase) ||
             el.GetString()!.Contains(WheelSpeedToken, StringComparison.OrdinalIgnoreCase));
    }
}
