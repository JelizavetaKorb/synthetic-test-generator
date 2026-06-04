using TestGen.Core.Models;

namespace TestGen.Core.Validation.Semantic;

/// <summary>
/// Rule 11/14 (§6.2): bus load plus expected background load must stay under
/// 100 %. The "background load" can be supplied either as a sibling parameter
/// (bus.backgroundLoadPct) or omitted (treated as 0).
/// </summary>
public sealed class BusLoadOverflowRule : IValidationRule
{
    public const string RuleCode = "BUS_LOAD_OVERFLOW";

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var p = scenario.Parameters;

        if (!ParameterAccessor.TryGetDouble(p, "bus.busLoadPct", out var busLoad) &&
            !ParameterAccessor.TryGetDouble(p, "busLoadPct",     out busLoad))
            yield break;

        ParameterAccessor.TryGetDouble(p, "bus.backgroundLoadPct", out var background);

        var total = busLoad + background;
        if (total > 100.0)
            yield return new ValidationIssue
            {
                Severity = "error",
                Code     = RuleCode,
                Field    = "bus.busLoadPct",
                Message  = $"Bus load {busLoad}% plus background {background}% = {total}% exceeds 100%. " +
                           "Reduce bus_load_pct or background load."
            };
        else if (total > 80.0)
            yield return new ValidationIssue
            {
                Severity = "warning",
                Code     = RuleCode,
                Field    = "bus.busLoadPct",
                Message  = $"Bus load {busLoad}% plus background {background}% = {total}% is above 80%, " +
                           "which may starve high-priority frames."
            };
    }
}
