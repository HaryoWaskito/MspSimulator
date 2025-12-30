# OCPI Handshake Control Page

## Overview
The Handshake page (`/Handshake`) provides a user-friendly interface for:
1. **Initiating EMSP-initiated handshakes** - Retrieve CPO versions and exchange credentials
2. **Revoking credentials** - Clear tokens and disconnect from CPO
3. **Observing handshake progress** - Step-by-step execution details
4. **Monitoring connection state** - Current status and token information

## URL Pattern
```
/Handshake
/Handshake/1          (for connection ID 1)
/Handshake/{id}       (for specific connection)
```

## Features

### Connection Information Section
Displays:
- **CPO Party ID / Country Code**: Unique CPO identifier
- **Base URL**: CPO's OCPI endpoint
- **Current Status**: Connection state (color-coded)
- **OCPI Token**: Masked CPO authentication token
- **Last Updated**: Timestamp of last state change

### Handshake Actions

#### Initiate EMSP Handshake
**Button**: "? Initiate EMSP Handshake"

**Workflow**:
1. Retrieve CPO versions from `/ocpi/versions`
2. Extract OCPI 2.3 credentials endpoint
3. Send EMSP credentials via POST to credentials endpoint
4. Store received CPO token
5. Update connection status to "Connected"

**Success Criteria**:
- HTTP 200 responses from both endpoints
- CPO token received and stored
- Connection status updated

**Failure Handling**:
- Logs HTTP errors (401, 403, 404, 5xx)
- Captures error response payloads
- Updates status to "HandshakeFailed"
- Displays detailed error messages

#### Revoke Credentials
**Button**: "? Revoke Credentials" (disabled if no token)

**Workflow**:
1. Clear OcpiToken from database
2. Clear ClientToken from database
3. Update connection status to "Revoked"
4. Update UpdatedAt timestamp

**Result**:
- Connection becomes inactive
- No tokens available for future calls
- Can reinitiate handshake to reconnect

### Result Display

When an action completes, shows:

**Success Result**:
```
? Success
Result: Handshake completed successfully
New Status: Connected
Steps Executed: [list of completed steps with checkmarks]
```

**Error Result**:
```
? Failed
Result: [error message]
Errors: [detailed error list]
Steps: [steps completed before failure]
```

## Status Color Coding

| Status | Color | Meaning |
|--------|-------|---------|
| None | Gray | Initial state |
| VersionsExchanged | Orange | Versions retrieved, pending credentials |
| Connected | Green | Handshake complete, ready for use |
| Revoked | Red | Credentials revoked |
| HandshakeFailed | Red | Handshake attempt failed |

## State Machine Transitions

```
None
  ?
GET /ocpi/versions (200)
  ?
VersionsExchanged
  ?
POST /ocpi/2.3.0/credentials (200)
  ?
Connected ?? Revoked
  (user revokes)
```

### Manual Revocation
From any state, user can click "Revoke Credentials" to immediately:
- Clear tokens
- Set status to "Revoked"
- Reset connection

## EMSP-Initiated Handshake Details

### Step 1: Get CPO Versions
```
GET https://cpo.example.com/ocpi/versions
```
- Discovers available OCPI versions
- Extracts endpoint URLs
- Looks for OCPI 2.3 availability

**Logged**: HTTP status, response count

### Step 2: Send Credentials to CPO
```
POST https://cpo.example.com/ocpi/2.3.0/credentials
Content-Type: application/json
X-Request-ID: [guid]
X-Correlation-ID: [guid]

{
  "token": "emsp_client_token",
  "url": "https://emsp.example.com/ocpi"
}
```
- Sends EMSP credentials to CPO
- Expects CPO credentials in response
- Stores CPO token for future calls

**Logged**: HTTP status, token received

### Step 3: Store and Confirm
- CPO token persisted to database
- Connection status updated to "Connected"
- UpdatedAt timestamp refreshed
- Page displays success message

## Error Handling

### Network Errors
- HTTP 401 Unauthorized: Authentication failed
- HTTP 403 Forbidden: Authorization failed
- HTTP 404 Not Found: Endpoint not available
- HTTP 5xx: CPO server error
- Timeout: Network unreachable

### Detailed Feedback
Each error includes:
- HTTP status code
- Full error response body (if available)
- Step where failure occurred
- Suggestions for remediation

### Example Error Scenario
```
? Failed
Result: Credentials exchange failed: HTTP 401

Errors:
  - Credentials endpoint returned 401
  - Response: {"statusCode":"2001","statusMessage":"Unauthorized"}

Steps:
  - Found connection: ABC/NL
  - Step 1: Retrieving CPO versions...
  - ? Versions retrieved: 1 version(s)
  - ? Credentials endpoint discovered: https://cpo.example.com/ocpi/2.3.0/credentials
  - Step 2: Sending EMSP credentials to CPO...
```

## Integration with Dashboard

The Handshake page complements the Dashboard:

**Dashboard** (`/Dashboard`):
- View connection state
- Configure error simulation flags
- See exchange audit logs

**Handshake** (`/Handshake`):
- Initiate/revoke handshakes
- Observe step-by-step progress
- Debug connection issues

## Step-by-Step Execution Display

Each action displays detailed progress:

```
Found connection: ABC/NL
Step 1: Retrieving CPO versions...
? Versions retrieved: 1 version(s)
? Credentials endpoint discovered: https://cpo.example.com/ocpi/2.3.0/credentials
Step 2: Sending EMSP credentials to CPO...
? Credentials sent to CPO
? CPO token received and stored
? Handshake completed successfully
```

User sees:
- What is happening at each stage
- Success/failure of each step
- Exact error messages on failure
- Time-ordered progression

## Database State Changes

### After Successful Handshake
```sql
UPDATE OcpiConnections SET
  Status = 'Connected',
  OcpiToken = '[cpo_token_from_exchange]',
  UpdatedAt = [current_utc_time]
WHERE Id = 1;
```

### After Credential Revocation
```sql
UPDATE OcpiConnections SET
  Status = 'Revoked',
  OcpiToken = NULL,
  ClientToken = NULL,
  UpdatedAt = [current_utc_time]
WHERE Id = 1;
```

## Logging

### Info Level
- User triggered handshake initiation
- User triggered credential revocation
- Handshake completion status

### Debug Level
- Full HTTP request/response payloads
- Token generation
- Step progression

Configure in `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "MspSimulator.Pages.HandshakeModel": "Information",
      "MspSimulator.Services.OcpiHandshakeService": "Information"
    }
  }
}
```

## Usage Workflow

### Initial Setup
1. Navigate to `/Handshake/1`
2. Verify CPO Base URL is correct
3. Confirm Current Status is "None"

### Initiate Handshake
1. Click "? Initiate EMSP Handshake"
2. Monitor step-by-step execution
3. Verify final status is "Connected"
4. Check "OCPI Token" field shows masked token

### Verify Success
1. Navigate to `/Dashboard/1`
2. Confirm Status is "Connected"
3. View "Last Exchange" timestamp

### Revoke Connection
1. Return to `/Handshake/1`
2. Click "? Revoke Credentials"
3. Verify status changes to "Revoked"
4. Observe tokens are cleared

### Reinitiate
1. From "Revoked" state, click handshake button again
2. Process repeats from beginning

## Error Recovery

### If Handshake Fails
1. Check error details on result panel
2. Navigate to Dashboard to enable error simulation if testing
3. Review HTTP response body for detailed error
4. Retry handshake after fixing root cause

### Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| 404 on versions | CPO URL incorrect | Verify BaseUrl in connection |
| 401 on credentials | Token expired | Revoke and reinitiate |
| Connection timeout | Network issue | Check CPO is reachable |
| Malformed response | CPO non-compliant | Check CPO OCPI 2.3 implementation |

## Advanced Features

### Token Generation
- If ClientToken not set, automatically generates 32-char GUID
- Persists to database on successful exchange

### Version Discovery
- Automatically finds OCPI 2.3 endpoint
- Constructs credentials URL dynamically
- Handles missing versions gracefully

### Masked Token Display
- Shows only first 4 and last 4 characters
- Prevents accidental exposure in logs/screenshots
- "(not set)" when no token available

## Security Considerations

- Page is admin/debug only (internal use)
- Tokens masked in UI display
- Raw payloads logged only at DEBUG level
- Revocation clears sensitive data
- No credentials exposed in URLs

## Testing

### Simulate Success
1. Ensure error flags unchecked on Dashboard
2. Click "Initiate EMSP Handshake"
3. Observe all steps complete successfully

### Simulate 401
1. Go to Dashboard
2. Enable "Simulate 401 Unauthorized on Credentials"
3. Click "Initiate EMSP Handshake" on Handshake page
4. Observe failure at Step 2

### Simulate 403
1. Go to Dashboard
2. Enable "Simulate 403 Forbidden on Credentials"
3. Click "Initiate EMSP Handshake"
4. Observe failure at Step 2

## Browser Compatibility

- Modern browsers (Chrome, Firefox, Safari, Edge)
- No JavaScript required
- Works with small viewport sizes
- Responsive table layouts
