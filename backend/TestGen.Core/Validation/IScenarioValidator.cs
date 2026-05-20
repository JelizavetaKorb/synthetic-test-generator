using TestGen.Core.Models;

namespace TestGen.Core.Validation;

public sealed class ValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<ValidationIssue> Issues { get; init; } = Array.Empty<ValidationIssue>();
}

public interface IScenarioValidator
{
    ValidationResult Validate(Scenario scenario, Profile profile);
}
