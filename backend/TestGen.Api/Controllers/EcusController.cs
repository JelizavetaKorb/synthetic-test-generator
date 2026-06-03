using Microsoft.AspNetCore.Mvc;
using TestGen.Core.Services;

namespace TestGen.Api.Controllers;

[ApiController]
[Route("api/ecus")]
public sealed class EcusController : ControllerBase
{
    private readonly IEcuCatalogService _catalog;

    public EcusController(IEcuCatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var ecus = _catalog.GetAll();
        return Ok(ecus);
    }
}
