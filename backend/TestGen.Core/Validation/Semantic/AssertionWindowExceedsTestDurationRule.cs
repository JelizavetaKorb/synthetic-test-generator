using System.Text.RegularExpressions;
using TestGen.Core.Models;

namespace TestGen.Core.Validation.Semantic;

public sealed class AssertionWindowExceedsTestDurationRule : IValidationRule
{
    public const string RuleCode = "ASSERTION_WINDOW_EXCEEDS_TEST_DURATION";

    private static readonly Regex AssertionIndexPattern =
        new(@"^assertions\[(\d+)\]\.", RegexOptions.Compiled);

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var p = scenario.Parameters;

        if (!ParameterAccessor.TryGetDouble(p, "testDurationMs", out var testDuration))
            yield break;

        var indices = new HashSet<int>();
        foreach (var key in p.Keys)
        {
            var m = AssertionIndexPattern.Match(key);
            if (m.Success && int.TryParse(m.Groups[1].Value, out var idx))
                indices.Add(idx);
        }

        foreach (var i in indices.OrderBy(static x => x))
        {
            if (!ParameterAccessor.TryGetDouble(p, $"assertions[{i}].timeWindowMs.to", out var windowEnd))
                if (!ParameterAccessor.TryGetDouble(p, $"assertions[{i}].time_window_ms.to", out windowEnd))
                    continue;

            if (windowEnd > testDuration)
            {
                ParameterAccessor.TryGetString(p, $"assertions[{i}].id", out var assertionId);
                yield return new ValidationIssue
                {
                    Severity = "error",
                    Code     = RuleCode,
                    Field    = $"assertions[{i}].time_window_ms",
                    Message  = $"Assertion '{assertionId}' observation window ends at {windowEnd}ms, which exceeds test duration of {testDuration}ms."
                };
            }
        }
    }
}
