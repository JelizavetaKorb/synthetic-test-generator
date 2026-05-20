using Microsoft.AspNetCore.Mvc;
using TestGen.Api.Contracts;
using TestGen.Core.Services;

namespace TestGen.Api.Controllers;

[ApiController]
[Route("api/profiles")]
public sealed class ProfilesController : ControllerBase
{
    private readonly IProfileService _profiles;
    private readonly ILogger<ProfilesController> _logger;

    public ProfilesController(IProfileService profiles, ILogger<ProfilesController> logger)
    {
        _profiles = profiles;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ProfileDto[]), StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        _logger.LogInformation("GET /api/profiles");
        var dtos = _profiles.GetAll().Select(ProfileDto.FromDomain).ToArray();
        return Ok(dtos);
    }
}
