using Microsoft.AspNetCore.Mvc;
using MspSimulator.Services;

namespace MspSimulator.Controllers;

[ApiController]
[Route("ui/error")]
public class ErrorSimulationController : ControllerBase
{
    private readonly IOcpiErrorSimulationService _errorSimulation;
    private readonly ILogger<ErrorSimulationController> _logger;

    public ErrorSimulationController(
        IOcpiErrorSimulationService errorSimulation,
        ILogger<ErrorSimulationController> logger)
    {
        _errorSimulation = errorSimulation;
        _logger = logger;
    }

    [HttpPost("401")]
    public async Task<IActionResult> Toggle401([FromQuery] bool? enable = null)
    {
        if (enable.HasValue)
        {
            await _errorSimulation.SetUnauthorizedAsync(enable.Value);
            _logger.LogInformation("Error simulation 401 set to {Value} via UI endpoint", enable.Value);
        }

        var (currentUnauth, currentForbid) = await _errorSimulation.GetCurrentSettingsAsync();

        return Ok(new
        {
            message = enable.HasValue ? "401 Unauthorized simulation updated" : "401 Unauthorized simulation status",
            forceUnauthorized = currentUnauth,
            forceForbidden = currentForbid,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("403")]
    public async Task<IActionResult> Toggle403([FromQuery] bool? enable = null)
    {
        if (enable.HasValue)
        {
            await _errorSimulation.SetForbiddenAsync(enable.Value);
            _logger.LogInformation("Error simulation 403 set to {Value} via UI endpoint", enable.Value);
        }

        var (currentUnauth, currentForbid) = await _errorSimulation.GetCurrentSettingsAsync();

        return Ok(new
        {
            message = enable.HasValue ? "403 Forbidden simulation updated" : "403 Forbidden simulation status",
            forceUnauthorized = currentUnauth,
            forceForbidden = currentForbid,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var (forceUnauthorized, forceForbidden) = await _errorSimulation.GetCurrentSettingsAsync();

        return Ok(new
        {
            forceUnauthorized,
            forceForbidden,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset()
    {
        await _errorSimulation.ResetAsync();
        _logger.LogInformation("Error simulation reset via UI endpoint");

        return Ok(new
        {
            message = "Error simulation reset to defaults",
            forceUnauthorized = false,
            forceForbidden = false,
            timestamp = DateTime.UtcNow
        });
    }
}
