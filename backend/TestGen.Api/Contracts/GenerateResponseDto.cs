using TestGen.Core.Models;

namespace TestGen.Api.Contracts;

public sealed class ScenarioDto
{
    public string ScenarioId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();

    public static ScenarioDto FromDomain(Scenario s) => new()
    {
        ScenarioId = s.ScenarioId,
        Name = s.Name,
        Description = s.Description,
        Parameters = s.Parameters
    };
}

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
    public ScenarioDto[] Scenarios { get; set; } = Array.Empty<ScenarioDto>();
    public ValidationIssueDto[] Issues { get; set; } = Array.Empty<ValidationIssueDto>();
}
