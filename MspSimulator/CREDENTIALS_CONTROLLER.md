# OCPI Credentials Controller

## Overview
The Credentials controller implements OCPI 2.3 credential exchange for the EMSP role. It handles bidirectional token exchange and connection state management.

## Endpoints

### POST /ocpi/2.3.0/credentials
**Purpose**: CPO sends credentials to EMSP; EMSP acknowledges and persists token.

**Request**:
```json
{
  "token": "CPO_TOKEN_VALUE",
  "url": "https://cpo.example.com/ocpi"
}
```

**Query Parameters**:
- `connectionId` (optional, default=1): Connection ID to update

**Success Response (200)**:
```json
{
  "statusCode": "1000",
  "statusMessage": null,
  "timestamp": "2024-01-15T10:30:00Z",
  "data": {
    "token": "EMSP_CLIENT_TOKEN",
    "url": "https://emsp.example.com/ocpi"
  }
}
```

**Error Responses**:
- `401 Unauthorized`: If `SimulateCredentialsUnauthorized` flag is set
- `403 Forbidden`: If `SimulateCredentialsForbidden` flag is set
- `404 Not Found`: If connection does not exist
- `400 Bad Request`: If token is missing or empty

**State Changes**:
- Updates connection `Status` to `"Connected"`
- Persists CPO token to `OcpiToken` field
- Stores raw request payload to `RawCredentialsPayload`
- Updates `UpdatedAt` timestamp

**Audit Trail**:
- Logs REQUEST with raw payload
- Logs RESPONSE with serialized response

---

### DELETE /ocpi/2.3.0/credentials
**Purpose**: EMSP revokes the connection; CPO token is cleared and state is reset.

**Query Parameters**:
- `connectionId` (optional, default=1): Connection ID to revoke

**Success Response (200)**:
```json
{
  "statusCode": "1000",
  "statusMessage": null,
  "timestamp": "2024-01-15T10:35:00Z",
  "data": null
}
```

**Error Responses**:
- `404 Not Found`: If connection does not exist

**State Changes**:
- Updates connection `Status` to `"Revoked"`
- Clears `OcpiToken` and `ClientToken` to null
- Updates `UpdatedAt` timestamp

**Audit Trail**:
- Logs REQUEST
- Logs RESPONSE with serialized response

---

## Error Simulation

Both endpoints support deterministic error simulation controlled via database flags:

| Flag | HTTP Status | Description |
|------|-------------|-------------|
| `SimulateCredentialsUnauthorized` | 401 | Authentication failure |
| `SimulateCredentialsForbidden` | 403 | Authorization failure |

Control these flags via:
1. **Dashboard UI**: `/Dashboard` - Toggle checkboxes and submit
2. **Direct SQL**: Update `OcpiConnections` table
3. **Programmatically**: Use EF Core to modify connection

---

## Connection State Transitions

```
None ? Connected (POST with valid token)
Connected ? Revoked (DELETE)
Any State ? 401/403 (Error simulation enabled)
```

---

## Raw Payload Persistence

Both request and response are stored verbatim in `CredentialExchangeLogs`:
- `Direction`: REQUEST or RESPONSE
- `Method`: POST or DELETE
- `Endpoint`: /ocpi/2.3.0/credentials
- `HttpStatusCode`: 200, 401, 403, or 404
- `RequestPayload`: Raw JSON request body (POST only)
- `ResponsePayload`: Serialized OCPI response

This enables complete audit trail and interoperability testing.

---

## Testing with cURL

**POST Credentials**:
```bash
curl -X POST "http://localhost:5000/ocpi/2.3.0/credentials?connectionId=1" \
  -H "Content-Type: application/json" \
  -d '{
    "token": "cpo_token_xyz",
    "url": "https://cpo.example.com/ocpi"
  }'
```

**DELETE Credentials**:
```bash
curl -X DELETE "http://localhost:5000/ocpi/2.3.0/credentials?connectionId=1"
```

---

## Integration with Versions Endpoint

The Credentials controller works in conjunction with the Versions endpoint:

1. **EMSP-Initiated Handshake**:
   - Client calls `GET /ocpi/versions` (upgrade connection status)
   - CPO calls `POST /ocpi/2.3.0/credentials` (send token)
   - EMSP responds with credentials

2. **CPO-Initiated Handshake**:
   - CPO calls `POST /ocpi/2.3.0/credentials` (send token first)
   - EMSP calls `GET /ocpi/versions` (then upgrade)
   - Sequence determined by `HandshakeMode` flag
