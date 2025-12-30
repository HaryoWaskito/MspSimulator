using MspSimulator.Ocpi.Client.Dtos;
using MspSimulator.Ocpi.Dtos;

namespace MspSimulator.Ocpi.Client;

public interface IOcpiHttpClient
{
    Task<OcpiClientResponse<OcpiVersionsResponse>> GetVersionsAsync(string cpoBaseUrl, string? token = null);

    Task<OcpiClientResponse<OcpiEndpointsResponse>> GetEndpointsAsync(string cpoBaseUrl, string? token = null);

    Task<OcpiClientResponse<OcpiCredentialsResponse>> PostCredentialsAsync(string credentialsUrl, OcpiCredential credentials, string? token = null);
}
