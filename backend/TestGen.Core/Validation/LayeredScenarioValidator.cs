using Microsoft.Extensions.Logging;
using TestGen.Core.Models;

namespace TestGen.Core.Validation;

public sealed class LayeredScenarioValidator : IScenarioValidator
{
    private readonly IReadOnlyList<IValidationRule> _rules;
    private readonly ILogger<LayeredScenarioValidator> _logger;

    public LayeredScenarioValidator(
        IEnumerable<IValidationRule> rules,
        ILogger<LayeredScenarioValidator> logger)
    {
        _rules  = rules.ToArray();
        _logger = logger;
    }

    public ValidationResult Validate(Scenario scenario, Profile profile)
    {
        var issues = new List<ValidationIssue>();

        foreach (var rule in _rules)
            issues.AddRange(rule.Check(scenario, profile));

        var errorCount   = issues.Count(i => string.Equals(i.Severity, "error",   StringComparison.OrdinalIgnoreCase));
        var warningCount = issues.Count(i => string.Equals(i.Severity, "warning", StringComparison.OrdinalIgnoreCase));

        _logger.LogDebug(
            "Validation for scenario {ScenarioId}: {Errors} error(s), {Warnings} warning(s).",
            scenario.ScenarioId, errorCount, warningCount);

        return new ValidationResult { IsValid = errorCount == 0, Issues = issues };
    }
}
