namespace TestGen.Core.Validation.Report;
public sealed class ValidationCheck
{
    public string Id       { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Field    { get; init; } = string.Empty;
    public string Status   { get; init; } = string.Empty;
    public string Message  { get; init; } = string.Empty;
}
