using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TestGen.Core.Validation.Report;
namespace TestGen.Api.Controllers;
[ApiController]
[Route("api/validate")]
public sealed class ValidationController : ControllerBase
{
    private readonly IScenarioReportValidator _validator;
    public ValidationController(IScenarioReportValidator validator)
    {
        _validator = validator;
    }
    [HttpPost]
    [ProducesResponseType(typeof(ScenarioValidationReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Validate([FromBody] JsonElement body)
    {
        if (body.ValueKind == JsonValueKind.Undefined || body.ValueKind == JsonValueKind.Null)
            return BadRequest(new { error = "Invalid JSON: body is null or undefined." });
        var report = _validator.Validate(body);
        return Ok(report);
    }
}
