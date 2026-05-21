namespace TestGen.Core.Validation.Report;
public sealed class ValidationSummary
{
    public int TotalChecks { get; init; }
    public int Passed      { get; init; }
    public int Failed      { get; init; }
    public int Warnings    { get; init; }
    public int Info        { get; init; }
}
public sealed class ScenarioValidationReport
{
    public ValidationSummary              Summary { get; init; } = new();
    public IReadOnlyList<ValidationCheck> Checks  { get; init; } = Array.Empty<ValidationCheck>();
}
