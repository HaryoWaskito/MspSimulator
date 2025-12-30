# OCPI Error Simulation Service

## Overview

The `IOcpiErrorSimulationService` provides a centralized, deterministic mechanism for simulating OCPI errors globally across all OCPI endpoints. It enables testing of error handling in CPO integrations by persisting error simulation flags to the database.

## Architecture

### Entity: `OcpiErrorSimulation`
```csharp
public class OcpiErrorSimulation
{
    public int Id { get; set; }
    public bool ForceUnauthorized { get; set; }    // Force 401 errors
    public bool ForceForbidden { get; set; }       // Force 403 errors
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

**Storage**: Single row in `OcpiErrorSimulations` table
- Singleton pattern - only one configuration instance
- Auto-created on first use
- Persisted across application restarts

### Interface: `IOcpiErrorSimulationService`
```csharp
public interface IOcpiErrorSimulationService
{
    Task<bool> IsUnauthorizedForcedAsync();
    Task<bool> IsForbiddenForcedAsync();
    Task SetUnauthorizedAsync(bool force);
    Task SetForbiddenAsync(bool force);
    Task SetBothAsync(bool forceUnauthorized, bool forceForbidden);
    Task<(bool ForceUnauthorized, bool ForceForbidden)> GetCurrentSettingsAsync();
    Task ResetAsync();
}
```

### Implementation: `OcpiErrorSimulationService`
Registered as scoped service in `Program.cs`:
```csharp
builder.Services.AddScoped<IOcpiErrorSimulationService, OcpiErrorSimulationService>();
```

## API Methods

### IsUnauthorizedForcedAsync
**Purpose**: Check if 401 Unauthorized is globally forced

**Returns**: `Task<bool>`
- `true`: All OCPI endpoints will return 401
- `false`: Normal operation

**Usage**:
```csharp
var forced = await _errorSimulation.IsUnauthorizedForcedAsync();
```

---

### IsForbiddenForcedAsync
**Purpose**: Check if 403 Forbidden is globally forced

**Returns**: `Task<bool>`
- `true`: All OCPI endpoints will return 403
- `false`: Normal operation

**Usage**:
```csharp
var forced = await _errorSimulation.IsForbiddenForcedAsync();
```

---

### SetUnauthorizedAsync
**Purpose**: Enable/disable global 401 Unauthorized forcing

**Parameters**:
- `force` (bool): `true` to enable, `false` to disable

**Behavior**:
- Updates `ForceUnauthorized` flag in database
- Sets `UpdatedAt` timestamp
- Logs at INFO level

**Usage**:
```csharp
await _errorSimulation.SetUnauthorizedAsync(true);   // Enable 401 forcing
await _errorSimulation.SetUnauthorizedAsync(false);  // Disable
```

---

### SetForbiddenAsync
**Purpose**: Enable/disable global 403 Forbidden forcing

**Parameters**:
- `force` (bool): `true` to enable, `false` to disable

**Behavior**:
- Updates `ForceForbidden` flag in database
- Sets `UpdatedAt` timestamp
- Logs at INFO level

**Usage**:
```csharp
await _errorSimulation.SetForbiddenAsync(true);   // Enable 403 forcing
await _errorSimulation.SetForbiddenAsync(false);  // Disable
```

---

### SetBothAsync
**Purpose**: Set both error simulation flags atomically

**Parameters**:
- `forceUnauthorized` (bool): Enable/disable 401 forcing
- `forceForbidden` (bool): Enable/disable 403 forcing

**Behavior**:
- Updates both flags in single database operation
- Sets `UpdatedAt` timestamp
- Logs at INFO level with both values
- Useful for atomic state changes

**Usage**:
```csharp
await _errorSimulation.SetBothAsync(
    forceUnauthorized: true,
    forceForbidden: false
);
```

---

### GetCurrentSettingsAsync
**Purpose**: Retrieve current error simulation configuration

**Returns**: `Task<(bool ForceUnauthorized, bool ForceForbidden)>`

**Behavior**:
- Returns tuple of current flag values
- Auto-creates configuration if not exists

**Usage**:
```csharp
var (unauthorized, forbidden) = await _errorSimulation.GetCurrentSettingsAsync();

if (unauthorized)
{
    // 401 forcing is enabled
}

if (forbidden)
{
    // 403 forcing is enabled
}
```

---

### ResetAsync
**Purpose**: Reset all error simulation flags to false

**Behavior**:
- Sets both flags to `false`
- Sets `UpdatedAt` timestamp
- Logs at INFO level
- Returns to normal operation

**Usage**:
```csharp
await _errorSimulation.ResetAsync();
```

## Integration with OCPI Endpoints

### Versions Endpoint
```csharp
// In OcpiVersionsService.GetVersionsAsync()

// Check global error simulation first
var (forceUnauthorized, forceForbidden) = 
    await _errorSimulation.GetCurrentSettingsAsync();

if (forceUnauthorized)
{
    return (401, new OcpiVersionsResponse(...));
}

if (forceForbidden)
{
    return (403, new OcpiVersionsResponse(...));
}

// Then check connection-specific flags...
```

### Credentials Endpoint
```csharp
// In OcpiCredentialsService.PostCredentialsAsync()

// Check global error simulation first
var (forceUnauthorized, forceForbidden) = 
    await _errorSimulation.GetCurrentSettingsAsync();

if (forceUnauthorized)
{
    return (401, new OcpiCredentialsResponse(...));
}

if (forceForbidden)
{
    return (403, new OcpiCredentialsResponse(...));
}

// Then check connection-specific flags...
```

## Error Simulation Precedence

Global error simulation takes precedence over connection-specific simulation:

```
1. Global ForceUnauthorized ? Return 401 (STOP)
2. Global ForceForbidden ? Return 403 (STOP)
3. Connection-specific ForceVersionsUnauthorized ? Return 401 (STOP)
4. Connection-specific ForceVersionsForbidden ? Return 403 (STOP)
5. Normal processing
```

This ensures global simulation overrides any connection-level configuration for complete control.

## Database Behavior

### Auto-creation
On first call, if no `OcpiErrorSimulation` record exists:
1. Creates new row with both flags = `false`
2. Sets `CreatedAt` to current UTC time
3. Returns created instance

```sql
INSERT INTO OcpiErrorSimulations 
  (ForceUnauthorized, ForceForbidden, CreatedAt) 
VALUES 
  (0, 0, datetime('now'))
```

### Updates
Each modification updates the row and `UpdatedAt`:
```sql
UPDATE OcpiErrorSimulations 
SET 
  ForceUnauthorized = 1,
  UpdatedAt = datetime('now')
WHERE Id = 1
```

### Single Instance
Only one row ever exists - the service queries `FirstOrDefault()` ensuring singleton pattern.

## Usage in Application

### In Razor Pages
```csharp
public class ErrorSimulationPageModel : PageModel
{
    private readonly IOcpiErrorSimulationService _errorSimulation;
    
    public bool ForceUnauthorized { get; set; }
    public bool ForceForbidden { get; set; }

    public ErrorSimulationPageModel(IOcpiErrorSimulationService errorSimulation)
    {
        _errorSimulation = errorSimulation;
    }

    public async Task OnGetAsync()
    {
        var (unauth, forbidden) = 
            await _errorSimulation.GetCurrentSettingsAsync();
        
        ForceUnauthorized = unauth;
        ForceForbidden = forbidden;
    }

    public async Task OnPostAsync()
    {
        await _errorSimulation.SetBothAsync(
            ForceUnauthorized,
            ForceForbidden
        );
        
        return RedirectToPage();
    }
}
```

### In API Controllers
```csharp
[ApiController]
[Route("api/error-simulation")]
public class ErrorSimulationController : ControllerBase
{
    private readonly IOcpiErrorSimulationService _errorSimulation;

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var (unauth, forbidden) = 
            await _errorSimulation.GetCurrentSettingsAsync();
        
        return Ok(new { ForceUnauthorized = unauth, ForceForbidden = forbidden });
    }

    [HttpPost("unauthorized/{force}")]
    public async Task<IActionResult> SetUnauthorized(bool force)
    {
        await _errorSimulation.SetUnauthorizedAsync(force);
        return Ok();
    }

    [HttpPost("forbidden/{force}")]
    public async Task<IActionResult> SetForbidden(bool force)
    {
        await _errorSimulation.SetForbiddenAsync(force);
        return Ok();
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset()
    {
        await _errorSimulation.ResetAsync();
        return Ok();
    }
}
```

### In Services
```csharp
public class OcpiVersionsService : IOcpiVersionsService
{
    private readonly IOcpiErrorSimulationService _errorSimulation;
    
    public async Task<OcpiVersionsResponse> GetVersionsAsync(int connectionId)
    {
        // Check global error simulation first
        var (forceUnauth, forbeForbid) = 
            await _errorSimulation.GetCurrentSettingsAsync();
        
        if (forceUnauth)
        {
            // Return 401 error response
        }
        
        if (forceForbid)
        {
            // Return 403 error response
        }
        
        // ... normal processing
    }
}
```

## Logging

The service logs at INFO level:

```
Global ForceUnauthorized set to True
Global ForceForbidden set to False
Global error simulation updated: ForceUnauthorized=True, ForceForbidden=False
Global error simulation reset to defaults
Created global error simulation configuration
```

Enable DEBUG logging for more verbose output:
```json
{
  "Logging": {
    "LogLevel": {
      "MspSimulator.Services.OcpiErrorSimulationService": "Debug"
    }
  }
}
```

## Testing Scenarios

### Test 401 Handling
```csharp
[Test]
public async Task Client_Handles_401_Correctly()
{
    // Arrange
    await errorSimulation.SetUnauthorizedAsync(true);
    
    // Act
    var response = await client.GetVersionsAsync(cpoUrl);
    
    // Assert
    Assert.AreEqual(401, response.StatusCode);
    
    // Cleanup
    await errorSimulation.ResetAsync();
}
```

### Test 403 Handling
```csharp
[Test]
public async Task Client_Handles_403_Correctly()
{
    // Arrange
    await errorSimulation.SetForbiddenAsync(true);
    
    // Act
    var response = await client.PostCredentialsAsync(url, creds);
    
    // Assert
    Assert.AreEqual(403, response.StatusCode);
    
    // Cleanup
    await errorSimulation.ResetAsync();
}
```

### Test Recovery
```csharp
[Test]
public async Task Client_Recovers_After_Reset()
{
    // Arrange - enable error
    await errorSimulation.SetUnauthorizedAsync(true);
    
    // Act - disable error
    await errorSimulation.ResetAsync();
    var response = await client.GetVersionsAsync(cpoUrl);
    
    // Assert
    Assert.AreEqual(200, response.StatusCode);
}
```

## Performance Considerations

- **Single-row query**: Minimal database overhead
- **FirstOrDefault() caching**: Consider implementing in-memory cache if high frequency
- **Async all the way**: Non-blocking database calls
- **Atomic operations**: `SetBothAsync()` prevents race conditions

## Deterministic Error Behavior

**Key characteristic**: Errors are **deterministic**, not random:
- Same error will occur on every call while flag is enabled
- No time-based behavior
- No probabilistic errors
- Fully controlled via database flags
- Repeatable test results

This aligns with Copilot Workspace Rules: "Deterministic only. No random or time-based behavior."

## Persistence Across Restarts

Error simulation flags persist in SQLite database:
1. Application starts
2. First OCPI call checks database for flags
3. Even if flags were enabled before restart, they remain enabled
4. Explicit `ResetAsync()` required to disable

This enables:
- Reproducible test runs
- Error state survives application restarts
- CI/CD integration for error scenario testing
