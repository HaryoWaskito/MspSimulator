using MspSimulator.Data;
using MspSimulator.Data.Entities;
using MspSimulator.Ocpi.Dtos;
using System.Text.Json;

namespace MspSimulator.Services;

public class OcpiCredentialsService : IOcpiCredentialsService
{
    private readonly OcpiDbContext _context;
    private readonly IOcpiErrorSimulationService _errorSimulation;
    private readonly ILogger<OcpiCredentialsService> _logger;

    public OcpiCredentialsService(
        OcpiDbContext context,
        IOcpiErrorSimulationService errorSimulation,
        ILogger<OcpiCredentialsService> logger)
    {
        _context = context;
        _errorSimulation = errorSimulation;
        _logger = logger;
    }

    public async Task<(int StatusCode, OcpiCredentialsResponse Response)> PostCredentialsAsync(
        int connectionId,
        OcpiCredential request,
        string rawPayload)
    {
        var connection = await _context.OcpiConnections.FindAsync(connectionId);
        if (connection == null)
        {
            return (404, new OcpiCredentialsResponse("2000", "Connection not found", DateTime.UtcNow, null));
        }

        // Check global error simulation flags first
        var (forceUnauthorized, forceForbidden) = await _errorSimulation.GetCurrentSettingsAsync();

        if (forceUnauthorized)
        {
            var errorResponse = new OcpiCredentialsResponse("2001", "Unauthorized", DateTime.UtcNow, null);
            await LogExchange(connection.Id, "REQUEST", "POST", "/ocpi/2.3/credentials", 401, rawPayload, null);
            await LogExchange(connection.Id, "RESPONSE", "POST", "/ocpi/2.3/credentials", 401, null, JsonSerializer.Serialize(errorResponse));
            _logger.LogInformation("Credentials endpoint: Forced 401 by global error simulation");
            return (401, errorResponse);
        }

        if (forceForbidden)
        {
            var errorResponse = new OcpiCredentialsResponse("2003", "Forbidden", DateTime.UtcNow, null);
            await LogExchange(connection.Id, "REQUEST", "POST", "/ocpi/2.3/credentials", 403, rawPayload, null);
            await LogExchange(connection.Id, "RESPONSE", "POST", "/ocpi/2.3/credentials", 403, null, JsonSerializer.Serialize(errorResponse));
            _logger.LogInformation("Credentials endpoint: Forced 403 by global error simulation");
            return (403, errorResponse);
        }

        // Check endpoint-specific error simulation flags
        if (connection.SimulateCredentialsUnauthorized)
        {
            var errorResponse = new OcpiCredentialsResponse("2001", "Unauthorized", DateTime.UtcNow, null);
            await LogExchange(connection.Id, "REQUEST", "POST", "/ocpi/2.3/credentials", 401, rawPayload, null);
            await LogExchange(connection.Id, "RESPONSE", "POST", "/ocpi/2.3/credentials", 401, null, JsonSerializer.Serialize(errorResponse));
            _logger.LogInformation("Credentials endpoint: Forced 401 by connection-specific simulation");
            return (401, errorResponse);
        }

        if (connection.SimulateCredentialsForbidden)
        {
            var errorResponse = new OcpiCredentialsResponse("2003", "Forbidden", DateTime.UtcNow, null);
            await LogExchange(connection.Id, "REQUEST", "POST", "/ocpi/2.3/credentials", 403, rawPayload, null);
            await LogExchange(connection.Id, "RESPONSE", "POST", "/ocpi/2.3/credentials", 403, null, JsonSerializer.Serialize(errorResponse));
            _logger.LogInformation("Credentials endpoint: Forced 403 by connection-specific simulation");
            return (403, errorResponse);
        }

        // Persist the token from CPO
        connection.OcpiToken = request.Token;
        connection.Status = "Connected";
        connection.UpdatedAt = DateTime.UtcNow;
        connection.RawCredentialsPayload = rawPayload;

        await _context.SaveChangesAsync();

        // Log the exchange
        await LogExchange(connection.Id, "REQUEST", "POST", "/ocpi/2.3/credentials", 200, rawPayload, null);

        // Build response with credential echo
        var responseData = new OcpiCredential(connection.ClientToken ?? "", connection.BaseUrl);
        var response = new OcpiCredentialsResponse("1000", null, DateTime.UtcNow, responseData);

        var responsePayload = JsonSerializer.Serialize(response);

        // Log the response
        await LogExchange(connection.Id, "RESPONSE", "POST", "/ocpi/2.3/credentials", 200, null, responsePayload);

        return (200, response);
    }

    public async Task<(int StatusCode, OcpiCredentialsResponse Response)> DeleteCredentialsAsync(int connectionId)
    {
        var connection = await _context.OcpiConnections.FindAsync(connectionId);
        if (connection == null)
        {
            return (404, new OcpiCredentialsResponse("2000", "Connection not found", DateTime.UtcNow, null));
        }

        // Revoke credentials
        connection.OcpiToken = null;
        connection.ClientToken = null;
        connection.Status = "Revoked";
        connection.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Log the deletion
        await LogExchange(connection.Id, "REQUEST", "DELETE", "/ocpi/2.3/credentials", 200, null, null);

        var response = new OcpiCredentialsResponse("1000", null, DateTime.UtcNow, null);

        var responsePayload = JsonSerializer.Serialize(response);

        // Log the response
        await LogExchange(connection.Id, "RESPONSE", "DELETE", "/ocpi/2.3/credentials", 200, null, responsePayload);

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