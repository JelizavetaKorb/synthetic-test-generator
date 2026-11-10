using TestGen.Core.Models;

namespace TestGen.Core.Validation.Semantic;

public sealed class AssertionWindowExceedsTestDurationRule : IValidationRule
{
    public const string RuleCode = "ASSERTION_WINDOW_EXCEEDS_TEST_DURATION";

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var p = scenario.Parameters;

        if (!ParameterAccessor.TryGetDouble(p, "testDurationMs", out var testDuration))
            yield break;

        foreach (var (i, assertion) in ParameterAccessor.GetItems(p, "assertions"))
        {
            if (!ParameterAccessor.TryGetDouble(assertion, "timeWindowMs.to", out var windowEnd) &&
                !ParameterAccessor.TryGetDouble(assertion, "time_window_ms.to", out windowEnd))
                continue;
            if (windowEnd <= testDuration) continue;

            ParameterAccessor.TryGetString(assertion, "id", out var assertionId);
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
