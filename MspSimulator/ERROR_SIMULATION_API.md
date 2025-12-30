# Error Simulation Control API

## Overview

Internal UI API endpoints for controlling deterministic OCPI error simulation during debugging and testing. These endpoints are **NOT part of OCPI** - they are administrative tools for the simulator.

**Base URL**: `/ui/error`
**Access**: Internal only (same application)

## Endpoints

### POST /ui/error/401
**Purpose**: Enable/disable global 401 Unauthorized error simulation

**Query Parameters**:
- `enable` (optional, bool): `true` to force 401, `false` to disable, omit to query status

**Request Examples**:

Enable 401 forcing:
```
POST /ui/error/401?enable=true
```

Disable 401 forcing:
```
POST /ui/error/401?enable=false
```

Query current status:
```
POST /ui/error/401
```

**Response (200 OK)**:
```json
{
  "message": "401 Unauthorized simulation updated",
  "forceUnauthorized": true,
  "forceForbidden": false,
  "timestamp": "2024-01-15T10:30:45.123456Z"
}
```

**Response when querying status** (no `enable` parameter):
```json
{
  "message": "401 Unauthorized simulation status",
  "forceUnauthorized": false,
  "forceForbidden": true,
  "timestamp": "2024-01-15T10:30:45.123456Z"
}
```

**Side Effects**:
- Updates database flag
- Logs action at INFO level
- Affects all subsequent OCPI endpoint calls
- Takes effect immediately

---

### POST /ui/error/403
**Purpose**: Enable/disable global 403 Forbidden error simulation

**Query Parameters**:
- `enable` (optional, bool): `true` to force 403, `false` to disable, omit to query status

**Request Examples**:

Enable 403 forcing:
```
POST /ui/error/403?enable=true
```

Disable 403 forcing:
```
POST /ui/error/403?enable=false
```

Query current status:
```
POST /ui/error/403
```

**Response (200 OK)**:
```json
{
  "message": "403 Forbidden simulation updated",
  "forceUnauthorized": false,
  "forceForbidden": true,
  "timestamp": "2024-01-15T10:30:45.123456Z"
}
```

**Response when querying status** (no `enable` parameter):
```json
{
  "message": "403 Forbidden simulation status",
  "forceUnauthorized": false,
  "forceForbidden": false,
  "timestamp": "2024-01-15T10:30:45.123456Z"
}
```

**Side Effects**:
- Updates database flag
- Logs action at INFO level
- Affects all subsequent OCPI endpoint calls
- Takes effect immediately

---

### GET /ui/error/status
**Purpose**: Query current error simulation status

**Query Parameters**: None

**Request**:
```
GET /ui/error/status
```

**Response (200 OK)**:
```json
{
  "forceUnauthorized": false,
  "forceForbidden": true,
  "timestamp": "2024-01-15T10:30:45.123456Z"
}
```

**Use Case**: Check which errors are currently forced without modifying state

---

### POST /ui/error/reset
**Purpose**: Disable all error simulations (reset to normal operation)

**Query Parameters**: None

**Request**:
```
POST /ui/error/reset
```

**Response (200 OK)**:
```json
{
  "message": "Error simulation reset to defaults",
  "forceUnauthorized": false,
  "forceForbidden": false,
  "timestamp": "2024-01-15T10:30:45.123456Z"
}
```

**Side Effects**:
- Sets both flags to `false`
- Logs action at INFO level
- Restores normal OCPI operation
- Takes effect immediately

---

## Usage Examples

### cURL Commands

**Enable 401 Unauthorized**:
```bash
curl -X POST "http://localhost:5000/ui/error/401?enable=true"
```

**Enable 403 Forbidden**:
```bash
curl -X POST "http://localhost:5000/ui/error/403?enable=true"
```

**Check current status**:
```bash
curl -X GET "http://localhost:5000/ui/error/status"
```

**Reset all errors**:
```bash
curl -X POST "http://localhost:5000/ui/error/reset"
```

---

### PowerShell Commands

**Enable 401**:
```powershell
Invoke-WebRequest -Uri "http://localhost:5000/ui/error/401?enable=true" `
    -Method Post | Select-Object -ExpandProperty Content | ConvertFrom-Json
```

**Check status**:
```powershell
Invoke-WebRequest -Uri "http://localhost:5000/ui/error/status" `
    -Method Get | Select-Object -ExpandProperty Content | ConvertFrom-Json
```

---

### JavaScript/Fetch

**Enable 401**:
```javascript
const response = await fetch('/ui/error/401?enable=true', {
    method: 'POST'
});
const data = await response.json();
console.log(data);
```

**Check status**:
```javascript
const response = await fetch('/ui/error/status');
const data = await response.json();
console.log(`401 forced: ${data.forceUnauthorized}`);
console.log(`403 forced: ${data.forceForbidden}`);
```

---

### C# HttpClient

**Enable 403**:
```csharp
using var client = new HttpClient();
var response = await client.PostAsync(
    "http://localhost:5000/ui/error/403?enable=true",
    null
);
var json = await response.Content.ReadAsStringAsync();
Console.WriteLine(json);
```

---

## Testing Workflow

### 1. Verify Normal Operation
```bash
# Check status
curl http://localhost:5000/ui/error/status
# Response: {"forceUnauthorized": false, "forceForbidden": false, ...}

# Call OCPI endpoint
curl http://localhost:5000/ocpi/versions
# Response: 200 OK with version data
```

### 2. Enable 401 Error
```bash
# Enable forcing
curl -X POST "http://localhost:5000/ui/error/401?enable=true"
# Response: {"forceUnauthorized": true, "forceForbidden": false, ...}

# Call OCPI endpoint
curl http://localhost:5000/ocpi/versions
# Response: 401 Unauthorized
```

### 3. Test Error Handling
```bash
# In your test code, verify:
# - Error response structure
# - Error status code (401)
# - Error message content
# - Proper error handling
```

### 4. Reset and Verify Recovery
```bash
# Disable forcing
curl -X POST "http://localhost:5000/ui/error/reset"
# Response: {"forceUnauthorized": false, "forceForbidden": false, ...}

# Call OCPI endpoint
curl http://localhost:5000/ocpi/versions
# Response: 200 OK with version data (normal operation)
```

---

## State Transitions

### 401 Forcing
```
POST /ui/error/401?enable=true
  ?
forceUnauthorized = true
  ?
All OCPI endpoints return 401
  ?
POST /ui/error/401?enable=false  (or POST /ui/error/reset)
  ?
forceUnauthorized = false
  ?
OCPI endpoints return normal responses
```

### 403 Forcing
```
POST /ui/error/403?enable=true
  ?
forceForbidden = true
  ?
All OCPI endpoints return 403
  ?
POST /ui/error/403?enable=false  (or POST /ui/error/reset)
  ?
forceForbidden = false
  ?
OCPI endpoints return normal responses
```

---

## Logging

All operations are logged at **INFO** level:

```
Error simulation 401 set to True via UI endpoint
Error simulation 403 set to False via UI endpoint
Error simulation reset via UI endpoint
```

Enable DEBUG logging to see more details:
```json
{
  "Logging": {
    "LogLevel": {
      "MspSimulator.Controllers.ErrorSimulationController": "Debug"
    }
  }
}
```

---

## Response Structure

All endpoints return consistent JSON responses:

```json
{
  "message": "Human-readable message",
  "forceUnauthorized": boolean,
  "forceForbidden": boolean,
  "timestamp": "ISO 8601 UTC timestamp"
}
```

Status endpoint returns simplified response:
```json
{
  "forceUnauthorized": boolean,
  "forceForbidden": boolean,
  "timestamp": "ISO 8601 UTC timestamp"
}
```

---

## HTTP Status Codes

| Code | Scenario |
|------|----------|
| 200 | Operation successful |
| 400 | Invalid query parameter |
| 500 | Server error |

---

## Database Persistence

All changes are immediately persisted to SQLite:

```sql
-- Enable 401
UPDATE OcpiErrorSimulations 
SET ForceUnauthorized = 1, UpdatedAt = datetime('now')

-- Enable 403
UPDATE OcpiErrorSimulations 
SET ForceForbidden = 1, UpdatedAt = datetime('now')

-- Reset
UPDATE OcpiErrorSimulations 
SET ForceUnauthorized = 0, ForceForbidden = 0, UpdatedAt = datetime('now')
```

Flags survive application restarts - changes are durable.

---

## Not Part of OCPI

These endpoints are **strictly internal UI endpoints**:
- Not part of OCPI 2.3 specification
- Not exposed to CPO
- For debugging and testing only
- Purely administrative
- No OCPI authentication required

---

## Integration with Dashboard

The Dashboard UI page can use these endpoints to control error simulation programmatically:

```javascript
// Enable 401 from UI
await fetch('/ui/error/401?enable=true', { method: 'POST' });

// Check status
const status = await fetch('/ui/error/status');
const data = await status.json();
document.getElementById('unauthorized').checked = data.forceUnauthorized;
```

---

## Testing with Automated Scripts

### Scenario: Test 401 Error Handling
```bash
#!/bin/bash

# Enable 401
curl -X POST "http://localhost:5000/ui/error/401?enable=true"

# Wait for propagation
sleep 1

# Test endpoint
response=$(curl -s -w "%{http_code}" http://localhost:5000/ocpi/versions)
status_code="${response: -3}"

if [ "$status_code" -eq 401 ]; then
    echo "? 401 error correctly returned"
else
    echo "? Expected 401, got $status_code"
fi

# Reset
curl -X POST "http://localhost:5000/ui/error/reset"
```

### Scenario: Test Recovery
```bash
#!/bin/bash

# Enable error
curl -X POST "http://localhost:5000/ui/error/403?enable=true"

# Verify error state
curl http://localhost:5000/ocpi/versions
# Returns: 403 Forbidden

# Reset
curl -X POST "http://localhost:5000/ui/error/reset"

# Verify recovery
curl http://localhost:5000/ocpi/versions
# Returns: 200 OK
```

---

## Error Scenarios

### Invalid Query Parameter
```
POST /ui/error/401?enable=invalid
```
Response: 400 Bad Request (ASP.NET Core model binding)

### Missing Endpoint
```
POST /ui/error/999
```
Response: 404 Not Found

### Database Error
If database is unavailable, endpoints return 500 Internal Server Error with details in logs.

---

## Security Notes

- These endpoints are **internal only** (not exposed externally)
- No authentication required (internal debug tools)
- Should be disabled or protected in production deployments
- Intended for development and testing only

---

## Combined Usage Example

Test complete handshake error scenario:

```bash
#!/bin/bash
set -e

echo "1. Enable 401 error"
curl -X POST "http://localhost:5000/ui/error/401?enable=true"
echo

echo "2. Attempt handshake (should fail with 401)"
curl http://localhost:5000/ocpi/versions
echo

echo "3. Check error status"
curl http://localhost:5000/ui/error/status
echo

echo "4. Disable 401 error"
curl -X POST "http://localhost:5000/ui/error/401?enable=false"
echo

echo "5. Retry handshake (should succeed)"
curl http://localhost:5000/ocpi/versions
echo

echo "6. Reset all errors"
curl -X POST "http://localhost:5000/ui/error/reset"
echo

echo "Done!"
```

---

## API Endpoint Summary

| Method | Endpoint | Purpose | Query Param |
|--------|----------|---------|------------|
| POST | /ui/error/401 | Control 401 simulation | enable (bool) |
| POST | /ui/error/403 | Control 403 simulation | enable (bool) |
| GET | /ui/error/status | Query current state | none |
| POST | /ui/error/reset | Reset to normal | none |

All endpoints are **internal UI API** and **not part of OCPI**.
