using TestGen.Core.Models;

namespace TestGen.Core.Validation.Semantic;

public sealed class TimingOverflowRule : IValidationRule
{
    public const string RuleCode = "TIMING_OVERFLOW";

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        var p = scenario.Parameters;

        if (!ParameterAccessor.TryGetDouble(p, "testDurationMs", out var testDuration))
            yield break;

        foreach (var (i, fault) in ParameterAccessor.GetItems(p, "faults"))
        {
            if (!ParameterAccessor.TryGetDouble(fault, "startTimeMs", out var start))    continue;
            if (!ParameterAccessor.TryGetDouble(fault, "durationMs",  out var duration)) continue;

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
