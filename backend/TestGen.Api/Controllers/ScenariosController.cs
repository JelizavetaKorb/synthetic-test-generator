using Microsoft.AspNetCore.Mvc;
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
    private readonly ILogger<ScenariosController> _logger;

    public ScenariosController(
        IGenerationService generation,
        IScenarioFormatter formatter,
        IProfileService profiles,
        ILogger<ScenariosController> logger)
    {
        _generation = generation;
        _formatter  = formatter;
        _profiles   = profiles;
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

        var profile = string.IsNullOrWhiteSpace(dto.ProfileId) ? null : _profiles.GetById(dto.ProfileId);
        var taken   = result.Scenarios.Take(dto.MaxScenarios).ToList();

        var scenarios = taken
            .Select(s =>
            {
                using var doc = _formatter.FormatOne(s, profile, request);
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
}
