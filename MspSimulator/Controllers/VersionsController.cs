using Microsoft.AspNetCore.Mvc;
using MspSimulator.Services;

namespace MspSimulator.Controllers;

[ApiController]
[Route("ocpi")]
public class VersionsController : ControllerBase
{
    private readonly IOcpiVersionsService _versionsService;
    private readonly ILogger<VersionsController> _logger;

    public VersionsController(IOcpiVersionsService versionsService, ILogger<VersionsController> logger)
    {
        _versionsService = versionsService;
        _logger = logger;
    }

    [HttpGet("versions")]
    public async Task<IActionResult> GetVersions([FromQuery] int connectionId = 1)
    {
        _logger.LogInformation("Versions request received for connection {ConnectionId}", connectionId);

        var (statusCode, response) = await _versionsService.GetVersionsAsync(connectionId);

        return statusCode switch
        {
            200 => Ok(response),
            401 => Unauthorized(response),
            403 => Forbid(),
            404 => NotFound(response),
            _ => StatusCode(500, response)
        };
    }
}
