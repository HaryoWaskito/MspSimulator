using MspSimulator.Data;
using MspSimulator.Ocpi.Client;
using MspSimulator.Ocpi.Dtos;

namespace MspSimulator.Services;

public interface IOcpiHandshakeService
{
    Task<HandshakeResult> InitiateEmspHandshakeAsync(int connectionId);
    Task<HandshakeResult> RevokeCredentialsAsync(int connectionId);
}

public class HandshakeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string Status { get; set; } = "";
    public List<string> Steps { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class OcpiHandshakeService : IOcpiHandshakeService
{
    private readonly IOcpiHttpClient _httpClient;
    private readonly OcpiDbContext _context;
    private readonly ILogger<OcpiHandshakeService> _logger;

    public OcpiHandshakeService(IOcpiHttpClient httpClient, OcpiDbContext context, ILogger<OcpiHandshakeService> logger)
    {
        _httpClient = httpClient;
        _context = context;
        _logger = logger;
    }

    public async Task<HandshakeResult> InitiateEmspHandshakeAsync(int connectionId)
    {
        var result = new HandshakeResult();

        try
        {
            var connection = await _context.OcpiConnections.FindAsync(connectionId);
            if (connection == null)
            {
                result.Success = false;
                result.Message = "Connection not found";
                result.Errors.Add($"Connection ID {connectionId} does not exist");
                return result;
            }

            result.Steps.Add($"Found connection: {connection.CpoPartyId}/{connection.CpoCountryCode}");

            // Step 1: Get CPO versions
            result.Steps.Add("Step 1: Retrieving CPO versions...");
            _logger.LogInformation("Initiating handshake: Getting versions from {BaseUrl}", connection.BaseUrl);

            var versionsResponse = await _httpClient.GetVersionsAsync(connection.BaseUrl, connection.OcpiToken);

            if (versionsResponse.StatusCode != 200)
            {
                result.Success = false;
                result.Message = $"Failed to retrieve CPO versions: HTTP {versionsResponse.StatusCode}";
                result.Errors.Add($"Versions endpoint returned {versionsResponse.StatusCode}");

                if (!string.IsNullOrEmpty(versionsResponse.RawPayload))
                {
                    result.Errors.Add($"Response: {versionsResponse.RawPayload}");
                }

                return result;
            }

            result.Steps.Add($"? Versions retrieved: {versionsResponse.Data?.Data?.Count ?? 0} version(s)");

            // Step 2: Extract credentials endpoint from versions
            var credentialsUrl = await ExtractCredentialsEndpointAsync(versionsResponse.Data, connection.OcpiToken!);
            if (string.IsNullOrEmpty(credentialsUrl))
            {
                result.Success = false;
                result.Message = "Credentials endpoint not found in CPO versions";
                result.Errors.Add("OCPI 2.3 endpoint not available");
                return result;
            }

            result.Steps.Add($"? Credentials endpoint discovered: {credentialsUrl}");

            // Step 3: Send credentials to CPO
            result.Steps.Add("Step 2: Sending EMSP credentials to CPO...");
            _logger.LogInformation("Sending credentials to {CredentialsUrl}", credentialsUrl);

            var emspCredentials = new OcpiCredential(
                Token: Guid.CreateVersion7().ToString("N"),
                Url: $"https://dev.msp-simulator.com/ocpi/{connection.Id}/",
                Roles: new[]
                {
                    new OcpiRole(
                        Role: "EMSP",
                        PartyId: "MSP",
                        CountryCode: "ID",
                        BusinessDetails: new OcpiBusinessDetails(
                            Name: "MSP Simulator",
                            Logo: null,
                            Website: null
                        )
                    )
                }
            );

            var credentialsResponse = await _httpClient.PostCredentialsAsync(credentialsUrl, emspCredentials, connection.OcpiToken);

            if (credentialsResponse.StatusCode != 200)
            {
                result.Success = false;
                result.Message = $"Credentials exchange failed: HTTP {credentialsResponse.StatusCode}";
                result.Errors.Add($"Credentials endpoint returned {credentialsResponse.StatusCode}");

                if (!string.IsNullOrEmpty(credentialsResponse.RawPayload))
                {
                    result.Errors.Add($"Response: {credentialsResponse.RawPayload}");
                }

                connection.Status = "HandshakeFailed";
                connection.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return result;
            }

            result.Steps.Add("? Credentials sent to CPO");

            // Step 4: Store CPO credentials
            if (credentialsResponse.Data?.Data != null)
            {
                connection.OcpiToken = credentialsResponse.Data.Data.Token;
                result.Steps.Add($"? CPO token received and stored");
            }

            connection.Status = "Connected";
            connection.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            result.Success = true;
            result.Message = "Handshake completed successfully";
            result.Status = "Connected";
            result.Steps.Add("? Handshake completed successfully");

            _logger.LogInformation("Handshake completed successfully for connection {Id}", connectionId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during handshake initiation for connection {ConnectionId}", connectionId);
            result.Success = false;
            result.Message = $"Handshake failed with exception: {ex.Message}";
            result.Errors.Add(ex.Message);
            return result;
        }
    }

    public async Task<HandshakeResult> RevokeCredentialsAsync(int connectionId)
    {
        var result = new HandshakeResult();

        try
        {
            var connection = await _context.OcpiConnections.FindAsync(connectionId);
            if (connection == null)
            {
                result.Success = false;
                result.Message = "Connection not found";
                result.Errors.Add($"Connection ID {connectionId} does not exist");
                return result;
            }

            result.Steps.Add($"Found connection: {connection.CpoPartyId}/{connection.CpoCountryCode}");

            // Call internal DELETE endpoint
            result.Steps.Add("Step 1: Revoking credentials...");
            _logger.LogInformation("Revoking credentials for connection {Id}", connectionId);

            // Clear tokens
            connection.OcpiToken = null;
            connection.ClientToken = null;
            connection.Status = "Revoked";
            connection.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            result.Success = true;
            result.Message = "Credentials revoked successfully";
            result.Status = "Revoked";
            result.Steps.Add("? Credentials revoked");
            result.Steps.Add("? Tokens cleared from database");

            _logger.LogInformation("Credentials revoked for connection {Id}", connectionId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking credentials for connection {ConnectionId}", connectionId);
            result.Success = false;
            result.Message = $"Revocation failed: {ex.Message}";
            result.Errors.Add(ex.Message);
            return result;
        }
    }

    private async Task<string?> ExtractCredentialsEndpointAsync(OcpiVersionsResponse? versionsResponse, string token)
    {
        if (versionsResponse?.Data == null || versionsResponse.Data.Count == 0)
        {
            return null;
        }

        // Look for OCPI 2.3 endpoint
        var version23 = versionsResponse.Data.FirstOrDefault(v => v.Version == "2.3.0");
        if (version23 == null)
        {
            return null;
        }

        var endpointResponse = await _httpClient.GetEndpointsAsync(version23.Url, token);

        var credentialEndpoint = endpointResponse.Data?.Data.Endpoints.FirstOrDefault(e => e.Identifier == "credentials");

        // Construct credentials endpoint
        return credentialEndpoint?.Url;
    }
}