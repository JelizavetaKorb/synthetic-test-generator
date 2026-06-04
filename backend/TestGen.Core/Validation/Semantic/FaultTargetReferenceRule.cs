using System.Text.Json;
using System.Text.RegularExpressions;
using TestGen.Core.Models;

namespace TestGen.Core.Validation.Semantic;

/// <summary>
/// Rule 5 / §6.2 #5: every fault.target must reference an existing signal
/// (signal_list[*].name) or message (messageDefs[*].id or name).
/// </summary>
public sealed class FaultTargetReferenceRule : IValidationRule
{
    public const string RuleCode = "FAULT_TARGET_UNKNOWN";

    private static readonly Regex FaultTargetPattern =
        new(@"^faults\[(\d+)\]\.target$", RegexOptions.Compiled);

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var p = scenario.Parameters;

        var knownTargets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in p.Keys)
        {
            if (Regex.IsMatch(key, @"^signalList\[\d+\]\.name$") ||
                Regex.IsMatch(key, @"^messageDefs\[\d+\]\.(id|name)$"))
            {
                if (ParameterAccessor.TryGetString(p, key, out var v) && !string.IsNullOrWhiteSpace(v))
                    knownTargets.Add(v);
            }
        }

        // Some payloads ship signal_list as a single JSON array (LIST_KEY_MAP path).
        if (p.TryGetValue("signalList", out var sigRaw) && sigRaw is JsonElement sigEl &&
            sigEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in sigEl.EnumerateArray())
                if (item.ValueKind == JsonValueKind.Object &&
                    item.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String)
                    knownTargets.Add(n.GetString()!);
        }
        if (p.TryGetValue("messageDefs", out var msgRaw) && msgRaw is JsonElement msgEl &&
            msgEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in msgEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;
                if (item.TryGetProperty("id",   out var id) && id.ValueKind == JsonValueKind.String)
                    knownTargets.Add(id.GetString()!);
                if (item.TryGetProperty("name", out var nm) && nm.ValueKind == JsonValueKind.String)
                    knownTargets.Add(nm.GetString()!);
            }
        }

        // If we couldn't discover any signals/messages at all, skip — the
        // scenario likely supplies them via a path we don't model and we'd
        // produce noisy false positives.
        if (knownTargets.Count == 0) yield break;

        foreach (var key in p.Keys)
        {
            var m = FaultTargetPattern.Match(key);
            if (!m.Success) continue;
            if (!ParameterAccessor.TryGetString(p, key, out var target) ||
                string.IsNullOrWhiteSpace(target)) continue;

            if (!knownTargets.Contains(target))
                yield return new ValidationIssue
                {
                    Severity = "error",
                    Code     = RuleCode,
                    Field    = key,
                    Message  = $"Fault target '{target}' is not defined in signal_list or messageDefs."
                };
        }
    }
}
