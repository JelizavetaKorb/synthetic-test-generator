using System.Text.RegularExpressions;
using TestGen.Core.Models;

namespace TestGen.Core.Validation.Semantic;

public sealed class TimingOverflowRule : IValidationRule
{
    public const string RuleCode = "TIMING_OVERFLOW";

    private static readonly Regex FaultIndexPattern =
        new(@"^faults\[(\d+)\]\.", RegexOptions.Compiled);

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var p = scenario.Parameters;

        if (!ParameterAccessor.TryGetDouble(p, "testDurationMs", out var testDuration))
            yield break;

        var indices = new HashSet<int>();
        foreach (var key in p.Keys)
        {
            var m = FaultIndexPattern.Match(key);
            if (m.Success && int.TryParse(m.Groups[1].Value, out var idx))
                indices.Add(idx);
        }

        foreach (var i in indices.OrderBy(static x => x))
        {
            if (!ParameterAccessor.TryGetDouble(p, $"faults[{i}].startTimeMs",  out var start))    continue;
            if (!ParameterAccessor.TryGetDouble(p, $"faults[{i}].durationMs",   out var duration)) continue;

            var end = start + duration;
            if (end > testDuration)
                yield return new ValidationIssue
                {
                    Severity = "error",
                    Code     = RuleCode,
                    Field    = $"faults[{i}].durationMs",
                    Message  = $"Fault {i} ends at {end} ms, beyond test duration {testDuration} ms."
                };
        }
    }
}
