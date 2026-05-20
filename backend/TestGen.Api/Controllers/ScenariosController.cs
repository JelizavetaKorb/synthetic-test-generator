using Microsoft.AspNetCore.Mvc;
using TestGen.Api.Contracts;
using TestGen.Core.Services;

namespace TestGen.Api.Controllers;

[ApiController]
[Route("api/scenarios")]
public sealed class ScenariosController : ControllerBase
{
    private readonly IGenerationService _generation;
    private readonly ILogger<ScenariosController> _logger;

    public ScenariosController(
        IGenerationService generation,
        ILogger<ScenariosController> logger)
    {
        _generation = generation;
        _logger = logger;
    }

    [HttpPost("generate")]
    [ProducesResponseType(typeof(GenerateResponseDto), StatusCodes.Status200OK)]
    public IActionResult Generate([FromBody] GenerateRequestDto dto)
    {
        _logger.LogInformation(
            "POST /api/scenarios/generate — profile={ProfileId} strategy={Strategy}",
            dto.ProfileId, dto.Strategy);

        var request = dto.ToDomain();
        var result = _generation.Generate(request);

        var scenarios = result.Scenarios.Take(dto.MaxScenarios).Select(ScenarioDto.FromDomain).ToArray();
        var issues = result.Issues.Select(ValidationIssueDto.FromDomain).ToArray();

        return Ok(new GenerateResponseDto
        {
            GeneratedCount = scenarios.Length,
            Scenarios = scenarios,
            Issues = issues
        });
    }
}
