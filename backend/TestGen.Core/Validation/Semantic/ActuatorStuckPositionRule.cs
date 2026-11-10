using TestGen.Core.Models;

namespace TestGen.Core.Validation.Semantic;

public sealed class ActuatorStuckPositionRule : IValidationRule
{
    public const string RuleCode = "ACTUATOR_POSITION_OUT_OF_RANGE";

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var p = scenario.Parameters;

        var ranges = new Dictionary<string, (double Min, double Max)>(StringComparer.Ordinal);
        foreach (var (_, signal) in ParameterAccessor.GetItems(p, "signalList"))
            if (ParameterAccessor.TryGetString(signal, "name", out var name) &&
                ParameterAccessor.TryGetDouble(signal, "min",  out var min) &&
                ParameterAccessor.TryGetDouble(signal, "max",  out var max))
                ranges[name] = (min, max);

        foreach (var (i, fault) in ParameterAccessor.GetItems(p, "faults"))
        {
            if (!ParameterAccessor.TryGetString(fault, "type", out var type) ||
                !type.Equals("actuator_stuck", StringComparison.OrdinalIgnoreCase)) continue;
            if (!ParameterAccessor.TryGetString(fault, "target", out var target) ||
                !ranges.TryGetValue(target, out var range)) continue;
            if (!ParameterAccessor.TryGetDouble(fault, "position", out var position)) continue;
            if (position >= range.Min && position <= range.Max) continue;

            yield return new ValidationIssue
            {
                Severity = "error",
                Code     = RuleCode,
                Field    = $"faults[{i}].position",
                Message  = $"actuator_stuck position {position} is outside signal '{target}' " +
                           $"range [{range.Min}..{range.Max}]."
            };
        }
    }
}
