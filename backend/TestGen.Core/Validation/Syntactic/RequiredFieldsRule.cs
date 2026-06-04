using TestGen.Core.Models;

namespace TestGen.Core.Validation.Syntactic;

public sealed class RequiredFieldsRule : IValidationRule
{
    public const string RuleCode = "REQUIRED_FIELD_MISSING";

    public IEnumerable<ValidationIssue> Check(Scenario scenario, Profile profile)
    {
        if (string.IsNullOrWhiteSpace(scenario.Name))
            yield return new ValidationIssue
            {
                Severity = "error",
                Code     = RuleCode,
                Field    = "name",
                Message  = "Scenario name is required."
            };

        if (!ParameterAccessor.TryGetString(scenario.Parameters, "ecuId", out var ecuId) ||
            string.IsNullOrWhiteSpace(ecuId))
            yield return new ValidationIssue
            {
                Severity = "error",
                Code     = RuleCode,
                Field    = "ecuId",
                Message  = "ecu_id is required: pick an ECU from the catalog."
            };

        if (!ParameterAccessor.TryGetString(scenario.Parameters, "targetExecution", out var exec) ||
            string.IsNullOrWhiteSpace(exec))
            yield return new ValidationIssue
            {
                Severity = "error",
                Code     = RuleCode,
                Field    = "targetExecution",
                Message  = "target_execution is required (simulation, SIL, HIL, bench or lab)."
            };
    }
}
