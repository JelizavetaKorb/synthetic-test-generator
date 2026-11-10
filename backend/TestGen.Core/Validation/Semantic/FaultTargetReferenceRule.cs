using TestGen.Core.Models;

namespace TestGen.Core.Validation.Semantic;

public sealed class FaultTargetReferenceRule : IValidationRule
{
    public const string RuleCode = "FAULT_TARGET_UNKNOWN";

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var p = scenario.Parameters;

        var knownTargets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (_, signal) in ParameterAccessor.GetItems(p, "signalList"))
            AddIfPresent(knownTargets, signal, "name");
        foreach (var (_, message) in ParameterAccessor.GetItems(p, "messageDefs"))
        {
            AddIfPresent(knownTargets, message, "id");
            AddIfPresent(knownTargets, message, "name");
        }

        if (knownTargets.Count == 0) yield break;

        foreach (var (i, fault) in ParameterAccessor.GetItems(p, "faults"))
        {
            if (!ParameterAccessor.TryGetString(fault, "target", out var target) ||
                string.IsNullOrWhiteSpace(target)) continue;

            if (!knownTargets.Contains(target))
                yield return new ValidationIssue
                {
                    Severity = "error",
                    Code     = RuleCode,
                    Field    = $"faults[{i}].target",
                    Message  = $"Fault target '{target}' is not defined in signal_list or messageDefs."
                };
        }
    }

    private static void AddIfPresent(HashSet<string> targets, IDictionary<string, object> item, string key)
    {
        if (ParameterAccessor.TryGetString(item, key, out var value) && !string.IsNullOrWhiteSpace(value))
            targets.Add(value);
    }
}
