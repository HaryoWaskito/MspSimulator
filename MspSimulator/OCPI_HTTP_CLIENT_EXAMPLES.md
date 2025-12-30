# OCPI HTTP Client Usage Examples

## Example 1: Simple Version Retrieval

```csharp
public class CpoDiscoveryService
{
    private readonly IOcpiHttpClient _ocpiClient;
    private readonly ILogger<CpoDiscoveryService> _logger;

    public CpoDiscoveryService(IOcpiHttpClient ocpiClient, ILogger<CpoDiscoveryService> logger)
    {
        _ocpiClient = ocpiClient;
        _logger = logger;
    }

    public async Task<List<string>> GetCpoVersionEndpointsAsync(string cpoBaseUrl)
    {
        var response = await _ocpiClient.GetVersionsAsync(cpoBaseUrl);

        if (response.StatusCode == 200 && response.Data?.Data != null)
        {
            return response.Data.Data.Select(v => v.Url).ToList();
        }

        _logger.LogWarning("Failed to retrieve versions from {CpoBaseUrl}: {StatusCode}", 
            cpoBaseUrl, response.StatusCode);
        
        return new List<string>();
    }
}
```

---

## Example 2: Credential Exchange with Error Handling

```csharp
public class CredentialExchangeService
{
    private readonly IOcpiHttpClient _ocpiClient;
    private readonly OcpiDbContext _context;
    private readonly ILogger<CredentialExchangeService> _logger;

    public CredentialExchangeService(
        IOcpiHttpClient ocpiClient,
        OcpiDbContext context,
        ILogger<CredentialExchangeService> logger)
    {
        _ocpiClient = ocpiClient;
        _context = context;
        _logger = logger;
    }

    public async Task<bool> ExchangeCredentialsAsync(int connectionId)
    {
        var connection = await _context.OcpiConnections.FindAsync(connectionId);
        if (connection == null)
        {
            _logger.LogError("Connection {ConnectionId} not found", connectionId);
            return false;
        }

        // Build credentials request
        var credentials = new OcpiCredentialsRequest(
            Token: connection.ClientToken ?? Guid.NewGuid().ToString(),
            Url: connection.BaseUrl
        );

        // Call CPO credentials endpoint
        var credentialsUrl = $"{connection.BaseUrl}/ocpi/2.3.0/credentials";
        var response = await _ocpiClient.PostCredentialsAsync(
            credentialsUrl,
            credentials,
            token: connection.OcpiToken);

        // Handle response
        return response.StatusCode switch
        {
            200 => HandleSuccessfulExchange(connection, response.Data),
            401 => HandleUnauthorized(connection, response),
            403 => HandleForbidden(connection, response),
            404 => HandleNotFound(connection, response),
            _ => HandleUnexpectedError(connection, response)
        };
    }

    private bool HandleSuccessfulExchange(
        OcpiConnection connection,
        OcpiCredentialsResponse? response)
    {
        if (response?.Data == null)
        {
            _logger.LogWarning("Successful response but missing credentials data");
            return false;
        }

        connection.OcpiToken = response.Data.Token;
        connection.Status = "Connected";
        connection.UpdatedAt = DateTime.UtcNow;

        _context.SaveChanges();
        _logger.LogInformation("Credentials exchanged successfully for connection {Id}", connection.Id);
        
        return true;
    }

    private bool HandleUnauthorized(OcpiConnection connection, OcpiClientResponse<OcpiCredentialsResponse> response)
    {
        _logger.LogWarning("Credentials exchange unauthorized: {Payload}", response.RawPayload);
        connection.Status = "AuthenticationFailed";
        _context.SaveChanges();
        return false;
    }

    private bool HandleForbidden(OcpiConnection connection, OcpiClientResponse<OcpiCredentialsResponse> response)
    {
        _logger.LogWarning("Credentials exchange forbidden: {Payload}", response.RawPayload);
        connection.Status = "AuthorizationFailed";
        _context.SaveChanges();
        return false;
    }

    private bool HandleNotFound(OcpiConnection connection, OcpiClientResponse<OcpiCredentialsResponse> response)
    {
        _logger.LogWarning("Credentials endpoint not found at CPO: {Payload}", response.RawPayload);
        return false;
    }

    private bool HandleUnexpectedError(OcpiConnection connection, OcpiClientResponse<OcpiCredentialsResponse> response)
    {
        _logger.LogError("Unexpected response from credentials endpoint: {StatusCode}", response.StatusCode);
        return false;
    }
}
```

---

## Example 3: Dual Handshake Orchestration

```csharp
public class OcpiHandshakeOrchestrator
{
    private readonly IOcpiHttpClient _ocpiClient;
    private readonly OcpiDbContext _context;
    private readonly ILogger<OcpiHandshakeOrchestrator> _logger;

    public OcpiHandshakeOrchestrator(
        IOcpiHttpClient ocpiClient,
        OcpiDbContext context,
        ILogger<OcpiHandshakeOrchestrator> logger)
    {
        _ocpiClient = ocpiClient;
        _context = context;
        _logger = logger;
    }

    public async Task<bool> InitiateEmspHandshakeAsync(int connectionId)
    {
        _logger.LogInformation("Starting EMSP-initiated handshake for connection {ConnectionId}", connectionId);

        var connection = await _context.OcpiConnections.FindAsync(connectionId);
        if (connection == null) return false;

        // Step 1: Get CPO versions
        var versionsResponse = await _ocpiClient.GetVersionsAsync(connection.BaseUrl);
        if (versionsResponse.StatusCode != 200)
        {
            _logger.LogError("Failed to retrieve CPO versions: {StatusCode}", versionsResponse.StatusCode);
            return false;
        }

        // Step 2: Extract credentials endpoint from versions
        var credentialsUrl = ExtractCredentialsUrl(versionsResponse.Data);
        if (string.IsNullOrEmpty(credentialsUrl))
        {
            _logger.LogError("Credentials endpoint not found in CPO versions");
            return false;
        }

        // Step 3: Send credentials to CPO
        var credentials = new OcpiCredentialsRequest(
            Token: connection.ClientToken ?? Guid.NewGuid().ToString(),
            Url: connection.BaseUrl
        );

        var credResponse = await _ocpiClient.PostCredentialsAsync(
            credentialsUrl,
            credentials);

        if (credResponse.StatusCode == 200 && credResponse.Data?.Data != null)
        {
            connection.OcpiToken = credResponse.Data.Data.Token;
            connection.Status = "Connected";
            connection.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("EMSP-initiated handshake completed successfully");
            return true;
        }

        _logger.LogError("Credential exchange failed: {StatusCode}", credResponse.StatusCode);
        return false;
    }

    public async Task<bool> HandleCpoInitiatedHandshakeAsync(int connectionId, OcpiCredentialsRequest cpoCredentials)
    {
        _logger.LogInformation("Handling CPO-initiated handshake for connection {ConnectionId}", connectionId);

        var connection = await _context.OcpiConnections.FindAsync(connectionId);
        if (connection == null) return false;

        // Store CPO token
        connection.OcpiToken = cpoCredentials.Token;
        connection.Status = "VersionsRequired";
        await _context.SaveChangesAsync();

        // Now retrieve versions with the CPO token
        var versionsResponse = await _ocpiClient.GetVersionsAsync(
            connection.BaseUrl,
            token: connection.OcpiToken);

        if (versionsResponse.StatusCode == 200)
        {
            connection.Status = "Connected";
            connection.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("CPO-initiated handshake completed successfully");
            return true;
        }

        _logger.LogError("Versions retrieval failed in CPO-initiated handshake: {StatusCode}", 
            versionsResponse.StatusCode);
        return false;
    }

    private string? ExtractCredentialsUrl(OcpiVersionsResponse? versionsData)
    {
        if (versionsData?.Data == null || versionsData.Data.Count == 0)
            return null;

        // Assume 2.3 is available; in production, negotiate version
        return versionsData.Data
            .FirstOrDefault(v => v.Version == "2.3")
            ?.Url + "/credentials";
    }
}
```

---

## Example 4: Integration in Razor Page

```csharp
// Pages/Handshake.cshtml.cs
public class HandshakeModel : PageModel
{
    private readonly OcpiHandshakeOrchestrator _orchestrator;
    private readonly ILogger<HandshakeModel> _logger;

    public string Message { get; set; } = "";

    public HandshakeModel(OcpiHandshakeOrchestrator orchestrator, ILogger<HandshakeModel> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task OnPostInitiateHandshakeAsync(int connectionId = 1)
    {
        var success = await _orchestrator.InitiateEmspHandshakeAsync(connectionId);
        
        Message = success 
            ? "Handshake completed successfully" 
            : "Handshake failed - check logs for details";
    }
}
```

---

## Example 5: Advanced Retry Logic (Manual Implementation)

```csharp
public class ResilientOcpiClient
{
    private readonly IOcpiHttpClient _ocpiClient;
    private readonly ILogger<ResilientOcpiClient> _logger;

    public ResilientOcpiClient(IOcpiHttpClient ocpiClient, ILogger<ResilientOcpiClient> logger)
    {
        _ocpiClient = ocpiClient;
        _logger = logger;
    }

    public async Task<OcpiClientResponse<OcpiVersionsResponse>> GetVersionsWithRetryAsync(
        string cpoBaseUrl,
        string? token = null,
        int maxRetries = 3)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var response = await _ocpiClient.GetVersionsAsync(cpoBaseUrl, token);
                
                if (response.StatusCode == 200)
                {
                    return response;
                }

                // Don't retry on client errors
                if (response.StatusCode >= 400 && response.StatusCode < 500)
                {
                    return response;
                }

                // Retry on server errors (5xx)
                _logger.LogWarning("Attempt {Attempt}: Received {StatusCode}, retrying...", 
                    attempt, response.StatusCode);
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Attempt {Attempt}: Network error, retrying...", attempt);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // exponential backoff
            }
        }

        _logger.LogError("All {Attempts} attempts failed", maxRetries);
        return new OcpiClientResponse<OcpiVersionsResponse>(0, null, null);
    }
}
```

---

## Key Points

1. **Always check StatusCode**: Don't assume success just because no exception was thrown
2. **Use RawPayload for debugging**: Error responses often contain useful information
3. **Log appropriately**: Use INFO for status, DEBUG for payloads
4. **Token management**: Store and reuse tokens from successful exchanges
5. **State transitions**: Update connection status based on exchange outcomes
6. **Non-throwing design**: Allows centralized error handling at a higher level

---

## Testing

```csharp
[TestFixture]
public class OcpiHttpClientTests
{
    private OcpiHttpClient _client;
    private Mock<HttpMessageHandler> _httpMessageHandler;

    [SetUp]
    public void Setup()
    {
        _httpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://test.example.com")
        };
        var logger = new Mock<ILogger<OcpiHttpClient>>();
        _client = new OcpiHttpClient(httpClient, logger.Object);
    }

    [Test]
    public async Task GetVersionsAsync_ReturnsSuccessResponse()
    {
        // Arrange
        var mockResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(@"
            {
              ""statusCode"": ""1000"",
              ""data"": [{ ""version"": ""2.3"", ""url"": ""https://cpo.example.com/ocpi/2.3"" }]
            }")
        };

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _client.GetVersionsAsync("https://cpo.example.com");

        // Assert
        Assert.AreEqual(200, result.StatusCode);
        Assert.NotNull(result.Data);
    }
}
```
