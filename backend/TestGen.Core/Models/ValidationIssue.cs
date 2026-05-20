namespace TestGen.Core.Models;

public sealed record ValidationIssue
{
    public string Severity { get; init; } = string.Empty;
    public string Code     { get; init; } = string.Empty;
    public string Field    { get; init; } = string.Empty;
    public string Message  { get; init; } = string.Empty;
}
