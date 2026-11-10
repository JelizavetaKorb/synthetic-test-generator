using System.Text.Json;
using TestGen.Core.Models;

namespace TestGen.Api.Contracts;

public sealed class ValidationIssueDto
{
    public string Severity { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public static ValidationIssueDto FromDomain(ValidationIssue i) => new()
    {
        Severity = i.Severity.ToString().ToLowerInvariant(),
        Code = i.Code,
        Field = i.Field,
        Message = i.Message
    };
}

public sealed class GenerateResponseDto
{
    public int GeneratedCount { get; set; }

    public JsonElement[] Scenarios { get; set; } = Array.Empty<JsonElement>();
    public ValidationIssueDto[] Issues { get; set; } = Array.Empty<ValidationIssueDto>();
}
