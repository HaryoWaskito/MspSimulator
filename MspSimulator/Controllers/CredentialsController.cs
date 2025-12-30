using Microsoft.AspNetCore.Mvc;
using MspSimulator.Ocpi.Dtos;
using MspSimulator.Services;

namespace MspSimulator.Controllers;

[ApiController]
[Route("ocpi/2.3.0")]
public class CredentialsController : ControllerBase
{
    private readonly IOcpiCredentialsService _credentialsService;
    private readonly ILogger<CredentialsController> _logger;

    public CredentialsController(IOcpiCredentialsService credentialsService, ILogger<CredentialsController> logger)
    {
        _credentialsService = credentialsService;
        _logger = logger;
    }

    [HttpPost("credentials")]
    public async Task<IActionResult> PostCredentials([FromBody] OcpiCredential request, [FromQuery] int connectionId = 1)
    {
        if (request == null)
        {
            _logger.LogWarning("Credentials POST received with null request for connection {ConnectionId}", connectionId);
            return BadRequest();
        }

        if (string.IsNullOrEmpty(request.Token))
        {
            _logger.LogWarning("Credentials POST received with empty token for connection {ConnectionId}", connectionId);
            return BadRequest("Token is required");
        }

        // Capture raw request body for persistence
        Request.EnableBuffering();
        var body = await new StreamReader(Request.Body).ReadToEndAsync();
        Request.Body.Position = 0;

        _logger.LogInformation("Credentials POST received for connection {ConnectionId}", connectionId);

        var (statusCode, response) = await _credentialsService.PostCredentialsAsync(connectionId, request, body);

        return statusCode switch
        {
            200 => Ok(response),
            400 => BadRequest(response),
            404 => NotFound(response),
            _ => StatusCode(500, response)
        };
    }

    [HttpDelete("credentials")]
    public async Task<IActionResult> DeleteCredentials([FromQuery] int connectionId = 1)
    {
        _logger.LogInformation("Credentials DELETE received for connection {ConnectionId}", connectionId);

        var (statusCode, response) = await _credentialsService.DeleteCredentialsAsync(connectionId);

        return statusCode switch
        {
            200 => Ok(response),
            404 => NotFound(response),
            _ => StatusCode(500, response)
        };
    }
}