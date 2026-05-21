using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using TestGen.Api.Contracts;
using TestGen.Core.Formatting;
using TestGen.Core.Services;

namespace TestGen.Api.Controllers;

[ApiController]
[Route("api/scenarios")]
public sealed class ScenariosController : ControllerBase
{
    private readonly IGenerationService _generation;
    private readonly IScenarioFormatter _formatter;
    private readonly IProfileService _profiles;
    private readonly IScenarioExporter _exporter;
    private readonly ILogger<ScenariosController> _logger;

    public ScenariosController(
        IGenerationService generation,
        IScenarioFormatter formatter,
        IProfileService profiles,
        IScenarioExporter exporter,
        ILogger<ScenariosController> logger)
    {
        _generation = generation;
        _formatter  = formatter;
        _profiles   = profiles;
        _exporter   = exporter;
        _logger     = logger;
    }

    [HttpPost("generate")]
    [ProducesResponseType(typeof(GenerateResponseDto), StatusCodes.Status200OK)]
    public IActionResult Generate([FromBody] GenerateRequestDto dto)
    {
        _logger.LogInformation(
            "POST /api/scenarios/generate — profile={ProfileId} strategy={Strategy}",
            dto.ProfileId, dto.Strategy);

        var request = dto.ToDomain();
        var result  = _generation.Generate(request);

        var profile = _profiles.GetById(dto.ProfileId);
        var taken   = result.Scenarios.Take(dto.MaxScenarios).ToList();

        var scenarios = taken
            .Select(s =>
            {
                using var doc = _formatter.FormatOne(s, profile!, request);
                return doc.RootElement.Clone();
            })
            .ToArray();

        var issues = result.Issues.Select(ValidationIssueDto.FromDomain).ToArray();

        return Ok(new GenerateResponseDto
        {
            GeneratedCount = scenarios.Length,
            Scenarios      = scenarios,
            Issues         = issues
        });
    }

    [HttpPost("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public IActionResult Export([FromBody] ExportRequestDto dto)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        if (dto.Format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var json  = JsonSerializer.Serialize(dto.Scenarios);
            var bytes = Encoding.UTF8.GetBytes(json);
            return File(bytes, "application/json", $"scenarios-{timestamp}.json");
        }

        if (dto.Format.Equals("csv", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = _exporter.ExportAsCsv(dto.Scenarios);
            return File(bytes, "text/csv", $"scenarios-{timestamp}.csv");
        }

        return Ok(new GenerateResponseDto
        {
            Issues = new[]
            {
                new ValidationIssueDto
                {
                    Severity = "error",
                    Code     = "UNSUPPORTED_EXPORT_FORMAT",
                    Field    = "format",
                    Message  = $"Format '{dto.Format}' is not supported. Use 'json' or 'csv'."
                }
            }
        });
    }
}
