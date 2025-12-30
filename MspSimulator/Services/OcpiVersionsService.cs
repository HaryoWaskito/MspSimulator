using MspSimulator.Data;
using MspSimulator.Data.Entities;
using MspSimulator.Ocpi.Dtos;
using System.Text.Json;

namespace MspSimulator.Services;

public class OcpiVersionsService : IOcpiVersionsService
{
    private readonly OcpiDbContext _context;
    private readonly IOcpiErrorSimulationService _errorSimulation;
    private readonly ILogger<OcpiVersionsService> _logger;

    public OcpiVersionsService(
        OcpiDbContext context,
        IOcpiErrorSimulationService errorSimulation,
        ILogger<OcpiVersionsService> logger)
    {
        _context = context;
        _errorSimulation = errorSimulation;
        _logger = logger;
    }

    public async Task<(int StatusCode, OcpiVersionsResponse Response)> GetVersionsAsync(int connectionId)
    {
        var connection = await _context.OcpiConnections.FindAsync(connectionId);
        if (connection == null)
        {
            return (404, new OcpiVersionsResponse(2000, "Connection not found", DateTime.UtcNow, new List<OcpiVersionInfo>()));
        }

        // Check global error simulation flags first
        var (forceUnauthorized, forceForbidden) = await _errorSimulation.GetCurrentSettingsAsync();

        if (forceUnauthorized)
        {
            var errorResponse = new OcpiVersionsResponse(2001, "Unauthorized", DateTime.UtcNow, new List<OcpiVersionInfo>());
            await LogExchange(connection.Id, "RESPONSE", "GET", "/ocpi/versions", 401, null, JsonSerializer.Serialize(errorResponse));
            _logger.LogInformation("Versions endpoint: Forced 401 by global error simulation");
            return (401, errorResponse);
        }

        if (forceForbidden)
        {
            var errorResponse = new OcpiVersionsResponse(2003, "Forbidden", DateTime.UtcNow, new List<OcpiVersionInfo>());
            await LogExchange(connection.Id, "RESPONSE", "GET", "/ocpi/versions", 403, null, JsonSerializer.Serialize(errorResponse));
            _logger.LogInformation("Versions endpoint: Forced 403 by global error simulation");
            return (403, errorResponse);
        }

        // Check endpoint-specific error simulation flags
        if (connection.SimulateVersionsUnauthorized)
        {
            var errorResponse = new OcpiVersionsResponse(2001, "Unauthorized", DateTime.UtcNow, new List<OcpiVersionInfo>());
            await LogExchange(connection.Id, "RESPONSE", "GET", "/ocpi/versions", 401, null, JsonSerializer.Serialize(errorResponse));
            _logger.LogInformation("Versions endpoint: Forced 401 by connection-specific simulation");
            return (401, errorResponse);
        }

        if (connection.SimulateVersionsForbidden)
        {
            var errorResponse = new OcpiVersionsResponse(2003, "Forbidden", DateTime.UtcNow, new List<OcpiVersionInfo>());
            await LogExchange(connection.Id, "RESPONSE", "GET", "/ocpi/versions", 403, null, JsonSerializer.Serialize(errorResponse));
            _logger.LogInformation("Versions endpoint: Forced 403 by connection-specific simulation");
            return (403, errorResponse);
        }

        // Build successful response
        var versionInfo = new OcpiVersionInfo("2.3", $"{connection.BaseUrl}/ocpi/2.3");
        var response = new OcpiVersionsResponse(1000, null, DateTime.UtcNow, new List<OcpiVersionInfo> { versionInfo });

        // Persist raw payload
        var rawPayload = JsonSerializer.Serialize(response);
        connection.RawVersionsPayload = rawPayload;
        connection.Status = "VersionsExchanged";
        connection.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Log exchange
        await LogExchange(connection.Id, "RESPONSE", "GET", "/ocpi/versions", 200, null, rawPayload);

        return (200, response);
    }

    private async Task LogExchange(
        int connectionId,
        string direction,
        string method,
        string endpoint,
        int statusCode,
        string? requestPayload,
        string? responsePayload)
    {
        var log = new CredentialExchangeLog
        {
            OcpiConnectionId = connectionId,
            Direction = direction,
            Method = method,
            Endpoint = endpoint,
            HttpStatusCode = statusCode,
            RequestPayload = requestPayload,
            ResponsePayload = responsePayload,
            Timestamp = DateTime.UtcNow
        };

        _context.CredentialExchangeLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}