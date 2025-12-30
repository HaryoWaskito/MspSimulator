using Microsoft.AspNetCore.Mvc.RazorPages;
using MspSimulator.Data;
using MspSimulator.Data.Entities;

namespace MspSimulator.Pages;

public class LogsModel : PageModel
{
    private readonly OcpiDbContext _context;
    private readonly ILogger<LogsModel> _logger;

    public OcpiConnection? Connection { get; set; }
    public List<CredentialExchangeLog> ExchangeLogs { get; set; } = new();
    public CredentialExchangeLog? SelectedLog { get; set; }

    public LogsModel(OcpiDbContext context, ILogger<LogsModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task OnGetAsync(int connectionId = 1, int? selectedLogId = null)
    {
        Connection = await _context.OcpiConnections.FindAsync(connectionId);

        if (Connection != null)
        {
            ExchangeLogs = _context.CredentialExchangeLogs
                .Where(l => l.OcpiConnectionId == connectionId)
                .OrderByDescending(l => l.Timestamp)
                .ToList();

            if (selectedLogId.HasValue)
            {
                SelectedLog = ExchangeLogs.FirstOrDefault(l => l.Id == selectedLogId.Value);
                _logger.LogInformation("User selected log entry {LogId} for connection {ConnectionId}", selectedLogId, connectionId);
            }
        }
    }

    public string FormatJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return "(empty)";
        }

        try
        {
            // Try to parse and re-format with indentation for readability
            var parsed = System.Text.Json.JsonDocument.Parse(json);
            return System.Text.Json.JsonSerializer.Serialize(
                parsed,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            // If not valid JSON, return as-is
            return json;
        }
    }

    public string GetStatusColor(int statusCode)
    {
        return statusCode switch
        {
            200 => "#28a745",      // Green
            201 => "#28a745",      // Green
            400 => "#ffc107",      // Yellow
            401 => "#dc3545",      // Red
            403 => "#dc3545",      // Red
            404 => "#ffc107",      // Yellow
            500 => "#dc3545",      // Red
            _ => "#666"            // Gray
        };
    }

    public string GetDirectionBadge(string direction)
    {
        if (direction == "REQUEST")
        {
            return "📤 REQUEST";
        }
        return "📥 RESPONSE";
    }
}