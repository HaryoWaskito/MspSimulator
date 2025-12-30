# OCPI Exchange Logs Page

## Overview

The Logs page (`/Logs`) provides a complete audit trail of all OCPI credential exchange operations. It displays:
- Chronological list of all exchanges (REQUEST/RESPONSE pairs)
- HTTP status codes and timestamps
- Selectable log entries with detailed payload inspection
- Formatted JSON display for request/response bodies

## URL Pattern
```
/Logs
/Logs/1          (for connection ID 1)
/Logs/{id}       (for specific connection)
/Logs/{id}?selectedLogId=5  (select specific log entry)
```

## Page Sections

### Connection Header
Displays current connection context:
- CPO Party ID / Country Code
- Base URL
- Total log entry count

### Exchange Log Table
Sortable list of all exchanges with columns:

| Column | Content | Purpose |
|--------|---------|---------|
| **Time** | ISO 8601 UTC timestamp | Exact timing of each exchange |
| **Direction** | ?? REQUEST or ?? RESPONSE | Inbound or outbound |
| **Method** | GET, POST, DELETE | HTTP method |
| **Endpoint** | /ocpi/versions, /ocpi/2.3/credentials | Target endpoint |
| **Status** | Color-coded HTTP status | Response code |
| **Action** | View button | Select entry to inspect |

**Ordering**: Most recent first (descending by timestamp)

**Styling**:
- Alternating rows for readability
- Color-coded status badges:
  - 200/201: Green (success)
  - 400/404: Yellow (client error)
  - 401/403/500: Red (authentication/server error)

### Log Entry Details (Conditional)
When a log entry is selected, displays:

**Metadata Section**:
- Timestamp (ISO 8601)
- Direction (REQUEST/RESPONSE)
- HTTP Method
- Endpoint
- HTTP Status Code
- Error Message (if present)

**Request Payload Section**:
- Raw JSON formatted with indentation
- Shows "(no request payload)" if empty
- Syntax highlighting via monospace font

**Response Payload Section**:
- Raw JSON formatted with indentation
- Shows "(no response payload)" if empty
- Syntax highlighting via monospace font

**Clear Selection Button**:
- Returns to log list without selection
- Resets to connection default view

### Navigation Section
Quick links to related pages:
- Dashboard (connection state)
- Handshake Control (trigger exchanges)

## Features

### No Pagination
All logs displayed in single list. Suitable for:
- Development and testing (limited logs)
- Debugging specific issues
- Complete audit trail visibility

### JSON Formatting
Automatically formats JSON for readability:
- Indentation applied
- Invalid JSON returned as-is
- Fallback handling for parse errors

### Log Selection
Click "View" button on any log entry to:
- Highlight the entry
- Display full metadata
- Show raw request and response payloads
- Allow payload inspection and copying

### Status Color Coding
HTTP status codes color-coded for quick scanning:
```
2xx (200, 201)  ? Green (#28a745) - Success
4xx (400, 404)  ? Yellow (#ffc107) - Client error
401/403         ? Red (#dc3545) - Auth error
5xx (500)       ? Red (#dc3545) - Server error
Other           ? Gray (#666) - Unknown
```

## Data Displayed

### From CredentialExchangeLog Entity
```csharp
public int Id                       // Log entry ID
public int OcpiConnectionId         // Parent connection
public string Direction             // REQUEST or RESPONSE
public string Method                // GET, POST, DELETE
public string Endpoint              // OCPI endpoint path
public int HttpStatusCode           // HTTP status
public string? RequestPayload       // Raw request JSON
public string? ResponsePayload      // Raw response JSON
public string? ErrorMessage         // Error description
public DateTime Timestamp           // Exchange time (UTC)
```

### Derived Information
- **Total count**: Number of exchanges for connection
- **Most recent**: Last exchange timestamp
- **Exchange pairs**: REQUEST followed by RESPONSE

## Typical Exchange Pattern

Each OCPI interaction creates multiple log entries:

**GET /ocpi/versions Exchange**:
```
[1] RESPONSE GET /ocpi/versions ? 200
    ?? Response: [...version data...]
```

**POST /ocpi/2.3/credentials Exchange**:
```
[2] REQUEST POST /ocpi/2.3/credentials ? 200
    ?? Request: {"token": "...", "url": "..."}
[3] RESPONSE POST /ocpi/2.3/credentials ? 200
    ?? Response: {"statusCode": "1000", "data": {...}}
```

**DELETE /ocpi/2.3/credentials Exchange**:
```
[4] REQUEST DELETE /ocpi/2.3/credentials ? 200
[5] RESPONSE DELETE /ocpi/2.3/credentials ? 200
    ?? Response: {"statusCode": "1000"}
```

## Usage Workflow

### 1. View All Logs
Navigate to `/Logs/1`:
- See table of all exchanges
- Ordered newest to oldest
- Status codes at a glance

### 2. Select an Entry
Click "View" on any row:
- Details panel appears below table
- Shows metadata, request, response
- JSON formatted for readability

### 3. Inspect Payloads
In details panel:
- Copy request JSON for analysis
- Check response structure
- Verify error messages (if any)

### 4. Clear Selection
Click "? Clear Selection":
- Returns to full log list
- Removes details panel
- Ready for next inspection

## JSON Formatting

### Before (Raw)
```
{"statusCode":"1000","statusMessage":null,"timestamp":"2024-01-15T10:30:00Z","data":[{"version":"2.3","url":"https://cpo.example.com/ocpi/2.3"}]}
```

### After (Formatted)
```json
{
  "statusCode": "1000",
  "statusMessage": null,
  "timestamp": "2024-01-15T10:30:00Z",
  "data": [
    {
      "version": "2.3",
      "url": "https://cpo.example.com/ocpi/2.3"
    }
  ]
}
```

## Error Scenarios

### No Logs Yet
Message: "No credential exchange logs found for this connection."
- Shown when ExchangeLogs.Count == 0
- Usually before first handshake attempt

### Malformed JSON
- Formatted version returned as-is
- No error thrown
- User sees raw content

### Missing Payloads
- REQUEST logs typically have no request (sent by external system)
- RESPONSE logs may lack response if error occurred
- Shown as "(no request/response payload)"

## Integration with Other Pages

### Dashboard
- "View Exchange Logs" button links to `/Logs`
- Shows total exchange count
- Quick access from connection state view

### Handshake Page
- Can trigger new exchanges
- Logs appear immediately on Logs page
- Allows debug-test-inspect workflow

## Database Persistence

All exchanges logged to `CredentialExchangeLogs` table:
```sql
SELECT * FROM CredentialExchangeLogs 
WHERE OcpiConnectionId = 1
ORDER BY Timestamp DESC
```

Logs survive:
- Application restarts
- Page navigation
- Connection state changes

## Performance Characteristics

- **No pagination**: All logs loaded on page load
- **In-memory sorting**: Done in PageModel (Order by descending)
- **Lazy details**: Only formatted when selected
- **Single connection**: Limited number of logs expected

**Suitable for**: Development, debugging, small test runs
**Not suitable for**: Production with millions of exchanges

## Logging Details

The PageModel logs at INFO level:
```
User selected log entry 5 for connection 1
```

Configure in `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "MspSimulator.Pages.LogsModel": "Information"
    }
  }
}
```

## Code Structure

### LogsModel (PageModel)
- `OnGetAsync(connectionId, selectedLogId)`: Load logs and optional selection
- `FormatJson(json)`: Format JSON with indentation
- `GetStatusColor(statusCode)`: Color for HTTP status
- `GetDirectionBadge(direction)`: Emoji badge for direction

### Logs.cshtml (View)
- Connection header with metadata
- Log table with sortable columns
- Conditional details panel
- Navigation links

## Debugging Tips

### Find Exchange with Specific Status Code
1. Look for the color-coded status badge
2. Scan table for red/yellow entries
3. Click View to inspect details

### Compare Request/Response Pairs
1. Click View on REQUEST entry
2. Note the timestamp and data
3. Find corresponding RESPONSE entry
4. Compare payloads

### Debug Handshake Failures
1. Navigate to Logs page
2. Look for 401/403 status codes
3. Inspect error message field
4. Review response payload
5. Adjust settings and retry

## Browser Compatibility

- Modern browsers (Chrome, Firefox, Safari, Edge)
- No JavaScript required
- Responsive table layouts
- Monospace font for code display

## Keyboard Navigation

- Tab through links and buttons
- Enter to select log entry
- Enter to clear selection
- No special keyboard shortcuts needed

## Accessibility

- Semantic HTML table structure
- Color coding supplemented with text labels
- Clear link text ("View", "? Dashboard")
- Sufficient color contrast

## Example Inspection Session

```
1. Navigate to /Logs/1
   ?
2. See 5 log entries in table
   ?
3. Find red 401 status entry
   ?
4. Click "View" button
   ?
5. Details panel shows:
   - HTTP Status: 401
   - Error: "Unauthorized"
   - Response JSON with error code
   ?
6. Diagnose: Error simulation flag is enabled
   ?
7. Click "? Clear Selection"
   ?
8. Go to Dashboard
   ?
9. Disable error simulation
   ?
10. Retry handshake
```

## State Persistence

Selection is URL-based:
```
/Logs/1                    (no selection)
/Logs/1?selectedLogId=5    (log 5 selected)
```

Allows:
- Bookmarking specific log entries
- Sharing inspection URLs
- Navigating back to selection

## Future Enhancements (Out of Scope)

Not implemented per Copilot Rules:
- Pagination
- Filtering by status code
- Filtering by timestamp range
- Export to CSV/JSON
- Search functionality
- Real-time log updates
