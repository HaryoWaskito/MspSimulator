using MspSimulator.Ocpi.Client.Dtos;
using MspSimulator.Ocpi.Dtos;
using System.Text;
using System.Text.Json;

namespace MspSimulator.Ocpi.Client;

public class OcpiHttpClient : IOcpiHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OcpiHttpClient> _logger;

    public OcpiHttpClient(HttpClient httpClient, ILogger<OcpiHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<OcpiClientResponse<OcpiVersionsResponse>> GetVersionsAsync(string cpoBaseUrl, string? token = null)
    {
        _logger.LogInformation("Sending GET {Url}", cpoBaseUrl);

        var request = new HttpRequestMessage(HttpMethod.Get, cpoBaseUrl);
        AttachOcpiHeaders(request, token);

        try
        {
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Received {StatusCode} from {Url}", response.StatusCode, cpoBaseUrl);
            _logger.LogDebug("Response body: {Body}", content);

            if (response.IsSuccessStatusCode)
            {
                var data = JsonSerializer.Deserialize<OcpiVersionsResponse>(content);

                return new OcpiClientResponse<OcpiVersionsResponse>((int)response.StatusCode, data, content);
            }
            else
            {
                return new OcpiClientResponse<OcpiVersionsResponse>(
                    (int)response.StatusCode,
                    null,
                    content
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling GET {Url}", cpoBaseUrl);
            throw;
        }
    }

    public async Task<OcpiClientResponse<OcpiEndpointsResponse>> GetEndpointsAsync(string cpoBaseUrl, string? token = null)
    {
        _logger.LogInformation("Sending GET {Url}", cpoBaseUrl);

        var request = new HttpRequestMessage(HttpMethod.Get, cpoBaseUrl);
        AttachOcpiHeaders(request, token);

        try
        {
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Received {StatusCode} from {Url}", response.StatusCode, cpoBaseUrl);
            _logger.LogDebug("Response body: {Body}", content);

            if (response.IsSuccessStatusCode)
            {
                var data = JsonSerializer.Deserialize<OcpiEndpointsResponse>(content);

                return new OcpiClientResponse<OcpiEndpointsResponse>((int)response.StatusCode, data, content);
            }
            else
            {
                return new OcpiClientResponse<OcpiEndpointsResponse>((int)response.StatusCode, null, content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling GET {Url}", cpoBaseUrl);
            throw;
        }
    }

    public async Task<OcpiClientResponse<OcpiCredentialsResponse>> PostCredentialsAsync(string credentialsUrl, OcpiCredential credentials, string? token = null)
    {
        _logger.LogInformation("Sending POST {Url}", credentialsUrl);

        var requestBody = JsonSerializer.Serialize(credentials);
        _logger.LogDebug("Request body: {Body}", requestBody);

        var request = new HttpRequestMessage(HttpMethod.Post, credentialsUrl)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };

        AttachOcpiHeaders(request, token);

        try
        {
            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Received {StatusCode} from {Url}", response.StatusCode, credentialsUrl);
            _logger.LogDebug("Response body: {Body}", content);

            if (response.IsSuccessStatusCode)
            {
                var data = JsonSerializer.Deserialize<OcpiCredentialsResponse>(content);
                return new OcpiClientResponse<OcpiCredentialsResponse>(
                    (int)response.StatusCode,
                    data,
                    content
                );
            }
            else
            {
                return new OcpiClientResponse<OcpiCredentialsResponse>(
                    (int)response.StatusCode,
                    null,
                    content
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling POST {Url}", credentialsUrl);
            throw;
        }
    }

    private static void AttachOcpiHeaders(HttpRequestMessage request, string? token)
    {
        request.Headers.Add("X-Request-ID", Guid.NewGuid().ToString());
        request.Headers.Add("X-Correlation-ID", Guid.NewGuid().ToString());

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Add("Authorization", $"Token {token}");
        }
    }
}