# OCPI HTTP Client

## Overview
The `IOcpiHttpClient` is a typed HTTP client used by the EMSP simulator to initiate outbound calls to CPO OCPI endpoints. It handles:
- Correct OCPI header attachment
- Raw payload logging for debugging
- Non-exception error handling (returns status codes)
- JSON serialization/deserialization

## Architecture

### Interface: `IOcpiHttpClient`
```csharp
public interface IOcpiHttpClient
{
    Task<OcpiClientResponse<OcpiVersionsResponse>> GetVersionsAsync(
        string cpoBaseUrl,
        string? token = null);

    Task<OcpiClientResponse<OcpiCredentialsResponse>> PostCredentialsAsync(
        string credentialsUrl,
        OcpiCredentialsRequest credentials,
        string? token = null);
}
```

### Implementation: `OcpiHttpClient`
Registered as a typed HttpClient in `Program.cs`:
```csharp
builder.Services.AddHttpClient<IOcpiHttpClient, OcpiHttpClient>()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });
```

---

## Endpoints

### GetVersionsAsync
**Purpose**: Retrieve available OCPI versions from CPO

**Parameters**:
- `cpoBaseUrl` (required): CPO's base URL (e.g., `https://cpo.example.com`)
- `token` (optional): OCPI authentication token

**Returns**:
```csharp
OcpiClientResponse<OcpiVersionsResponse>
{
    StatusCode: int,           // HTTP status (200, 401, 403, 404, etc.)
    Data: OcpiVersionsResponse?, // Deserialized response (null if error)
    RawPayload: string?         // Raw JSON response body
}
```

**OCPI Headers Attached**:
- `X-Request-ID`: Unique request identifier (GUID)
- `X-Correlation-ID`: Correlation identifier (GUID)
- `Authorization: Token {token}` (if token provided)

**Logging**:
- INFO: Request URL
- INFO: Response status code
- DEBUG: Full response body

**Error Handling**:
- Does NOT throw on 401/403/404
- Returns status code and null Data
- Throws on network/timeout exceptions

**Example Usage**:
```csharp
var client = /* injected IOcpiHttpClient */;
var response = await client.GetVersionsAsync("https://cpo.example.com", token: "cpo_token_xyz");

if (response.StatusCode == 200)
{
    // Parse response.Data
}
else
{
    // Handle error response
    var errorBody = response.RawPayload;
}
```

---

### PostCredentialsAsync
**Purpose**: Send EMSP credentials to CPO for credential exchange

**Parameters**:
- `credentialsUrl` (required): CPO's credentials endpoint URL (e.g., `https://cpo.example.com/ocpi/2.3.0/credentials`)
- `credentials` (required): Credential payload containing:
  - `Token`: EMSP's client token
  - `Url`: EMSP's OCPI endpoint URL
- `token` (optional): OCPI authentication token

**Request Body**:
```json
{
  "token": "emsp_client_token_abc123",
  "url": "https://emsp.example.com/ocpi"
}
```

**Returns**:
```csharp
OcpiClientResponse<OcpiCredentialsResponse>
{
    StatusCode: int,              // HTTP status (200, 401, 403, 404, etc.)
    Data: OcpiCredentialsResponse?, // Deserialized response (null if error)
    RawPayload: string?            // Raw JSON response body
}
```

**OCPI Headers Attached**:
- `X-Request-ID`: Unique request identifier (GUID)
- `X-Correlation-ID`: Correlation identifier (GUID)
- `Authorization: Token {token}` (if token provided)
- `Content-Type: application/json` (automatic)

**Logging**:
- INFO: POST URL
- DEBUG: Request body (raw JSON)
- INFO: Response status code
- DEBUG: Response body (raw JSON)

**Error Handling**:
- Does NOT throw on 401/403/404
- Returns status code and null Data
- Throws on network/timeout exceptions
- Always includes raw payload for debugging

**Example Usage**:
```csharp
var client = /* injected IOcpiHttpClient */;
var credentials = new OcpiCredentialsRequest(
    Token: "emsp_token_def456",
    Url: "https://emsp.example.com/ocpi"
);

var response = await client.PostCredentialsAsync(
    "https://cpo.example.com/ocpi/2.3.0/credentials",
    credentials,
    token: "previous_cpo_token"
);

if (response.StatusCode == 200)
{
    // Credential exchange successful
    var cpoCredentials = response.Data;
}
else if (response.StatusCode == 401)
{
    // Unauthorized - parse error response
    var errorResponse = JsonSerializer.Deserialize<OcpiCredentialsResponse>(response.RawPayload);
}
```

---

## OCPI Header Specification

### X-Request-ID
- **Purpose**: Unique identifier for this request
- **Value**: GUID (generated per request)
- **Used**: Tracing individual requests

### X-Correlation-ID
- **Purpose**: Correlate related requests in a conversation
- **Value**: GUID (generated per request, could be linked in conversation)
- **Used**: Tracing request chains

### Authorization
- **Format**: `Token {token_value}`
- **Sent Only If**: Token parameter is provided
- **OCPI 2.3 Spec**: Standard authentication mechanism

---

## Response Structure

### Success (2xx)
```csharp
new OcpiClientResponse<T>(
    StatusCode: 200,
    Data: /* deserialized OCPI response */,
    RawPayload: /* raw JSON string */
)
```

### Error (4xx/5xx)
```csharp
new OcpiClientResponse<T>(
    StatusCode: 401,  // or 403, 404, 500, etc.
    Data: null,       // Not deserialized on error
    RawPayload: /* raw error response JSON */
)
```

**Note**: Raw payload is ALWAYS captured, enabling:
- Manual error response parsing
- Compliance testing
- Debugging integration issues

---

## Configuration

### Timeout
Default: 30 seconds per request

Override in `Program.cs`:
```csharp
builder.Services.AddHttpClient<IOcpiHttpClient, OcpiHttpClient>()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(60);
    });
```

### Logging Levels
- **INFO**: Request/Response status
- **DEBUG**: Full request/response bodies (verbose, disabled by default)

Configure in `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "MspSimulator.Ocpi.Client.OcpiHttpClient": "Debug"
    }
  }
}
```

---

## Handshake Integration

### EMSP-Initiated Handshake
```
1. EMSP: GetVersionsAsync(cpoBaseUrl)
   ? Receives CPO version endpoints
   
2. CPO: POST /ocpi/2.3.0/credentials
   ? Sends CPO credentials to EMSP
   
3. EMSP: POST /ocpi/2.3.0/credentials response
   ? Returns EMSP credentials (via CredentialsController)
```

### CPO-Initiated Handshake
```
1. CPO: POST /ocpi/2.3.0/credentials
   ? Sends CPO credentials to EMSP
   
2. EMSP: GetVersionsAsync(cpoBaseUrl, token)
   ? Now uses CPO token to call versions
   
3. CPO: Continues with handshake
```

---

## Testing with cURL

**GET Versions**:
```bash
curl -X GET "https://cpo.example.com/ocpi/versions" \
  -H "X-Request-ID: $(uuidgen)" \
  -H "X-Correlation-ID: $(uuidgen)" \
  -H "Authorization: Token cpo_token_xyz"
```

**POST Credentials**:
```bash
curl -X POST "https://cpo.example.com/ocpi/2.3.0/credentials" \
  -H "Content-Type: application/json" \
  -H "X-Request-ID: $(uuidgen)" \
  -H "X-Correlation-ID: $(uuidgen)" \
  -H "Authorization: Token cpo_token_xyz" \
  -d '{
    "token": "emsp_client_token",
    "url": "https://emsp.example.com/ocpi"
  }'
```

---

## Error Scenarios

| Scenario | Behavior | Returns |
|----------|----------|---------|
| 200 Response | Deserialize data | `StatusCode=200, Data=deserialized, RawPayload=json` |
| 401 Response | Don't throw | `StatusCode=401, Data=null, RawPayload=error_json` |
| 403 Response | Don't throw | `StatusCode=403, Data=null, RawPayload=error_json` |
| 404 Response | Don't throw | `StatusCode=404, Data=null, RawPayload=error_json` |
| Timeout | Throw exception | `HttpRequestException` |
| Network error | Throw exception | `HttpRequestException` |
| Malformed JSON | Log & continue | `StatusCode=200, Data=null, RawPayload=json` |

---

## Dependency Injection

### Inject into Services
```csharp
public class MyService
{
    private readonly IOcpiHttpClient _ocpiClient;
    
    public MyService(IOcpiHttpClient ocpiClient)
    {
        _ocpiClient = ocpiClient;
    }
    
    public async Task DoSomething()
    {
        var response = await _ocpiClient.GetVersionsAsync("https://cpo.example.com");
    }
}
```

### Inject into Controllers
```csharp
[ApiController]
public class MyController : ControllerBase
{
    private readonly IOcpiHttpClient _ocpiClient;
    
    public MyController(IOcpiHttpClient ocpiClient)
    {
        _ocpiClient = ocpiClient;
    }
}
```

### Inject into Razor Pages
```csharp
public class MyPageModel : PageModel
{
    private readonly IOcpiHttpClient _ocpiClient;
    
    public MyPageModel(IOcpiHttpClient ocpiClient)
    {
        _ocpiClient = ocpiClient;
    }
}
```
