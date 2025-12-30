using MspSimulator.Data;
using MspSimulator.Data.Entities;
using MspSimulator.Ocpi.Dtos;
using System.Text.Json;

namespace MspSimulator.Services;

public class OcpiVersionsService : IOcpiVersionsService
{
    private readonly OcpiDbContext _context;

    public OcpiVersionsService(OcpiDbContext context)
    {
        _context = context;
    }

    public async Task<(int StatusCode, OcpiVersionsResponse Response)> GetVersionsAsync(int connectionId)
    {
        var connection = await _context.OcpiConnections.FindAsync(connectionId);
        if (connection == null)
        {
            return (404, new OcpiVersionsResponse(2000, "Connection not found", DateTime.UtcNow, new List<OcpiVersionInfo>()));
        }

        // Check error simulation flags
        if (connection.SimulateVersionsUnauthorized)
        {
            var errorResponse = new OcpiVersionsResponse(2001, "Unauthorized", DateTime.UtcNow, new List<OcpiVersionInfo>());
            await LogExchange(connection.Id, "RESPONSE", "GET", "/ocpi/versions", 401, null, JsonSerializer.Serialize(errorResponse));
            return (401, errorResponse);
        }

        if (connection.SimulateVersionsForbidden)
        {
            var errorResponse = new OcpiVersionsResponse(2003, "Forbidden", DateTime.UtcNow, new List<OcpiVersionInfo>());
            await LogExchange(connection.Id, "RESPONSE", "GET", "/ocpi/versions", 403, null, JsonSerializer.Serialize(errorResponse));
            return (403, errorResponse);
        }

        // Build successful response
        var versionInfo = new OcpiVersionInfo("2.3", $"{connection.BaseUrl}/ocpi/2.3");

        var response = new OcpiVersionsResponse(1000, null, DateTime.UtcNow, new List<OcpiVersionInfo> { versionInfo }
        );

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

    private async Task LogExchange(int connectionId, string direction, string method, string endpoint, int statusCode,
                                   string? requestPayload, string? responsePayload)
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