# Database Migration Guide

To initialize the SQLite database for the OCPI Simulator, run the following commands from the package manager console:

## Initial Migration

```powershell
Add-Migration InitialCreate
Update-Database
```

This will create the following tables:
- `OcpiConnections` - CPO connection state and configuration
- `CredentialExchangeLogs` - Audit trail of all OCPI exchanges

## Insert Sample CPO Connection

After migration, insert a sample CPO connection for testing:

```sql
INSERT INTO OcpiConnections (
    CpoPartyId,
    CpoCountryCode,
    BaseUrl,
    Status,
    HandshakeMode,
    IsActive,
    CreatedAt
) VALUES (
    'ABC',
    'NL',
    'https://cpo.example.com/ocpi',
    'None',
    0,
    1,
    datetime('now')
);
```

## Database Schema

### OcpiConnections
- `Id` (int, PK)
- `CpoPartyId` (varchar(3), required)
- `CpoCountryCode` (varchar(2), required)
- `BaseUrl` (varchar(500), required)
- `OcpiToken` (varchar(500))
- `ClientToken` (varchar(500))
- `CreatedAt` (datetime, default: now)
- `UpdatedAt` (datetime)
- `IsActive` (bool, default: true)
- `HandshakeMode` (int, default: 0)
- `Status` (varchar(50), default: 'None')
- `SimulateVersionsUnauthorized` (bool, default: false)
- `SimulateVersionsForbidden` (bool, default: false)
- `RawVersionsPayload` (text)
- `RawCredentialsPayload` (text)

### CredentialExchangeLogs
- `Id` (int, PK)
- `OcpiConnectionId` (int, FK)
- `Direction` (varchar(10), required)
- `Method` (varchar(10), required)
- `Endpoint` (varchar(500), required)
- `HttpStatusCode` (int, required)
- `RequestPayload` (text)
- `ResponsePayload` (text)
- `ErrorMessage` (varchar(500))
- `Timestamp` (datetime, default: now)
