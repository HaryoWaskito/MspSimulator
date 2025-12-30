using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MspSimulator.Data;
using MspSimulator.Data.Entities;

namespace MspSimulator.Pages;

public class DashboardModel : PageModel
{
    private readonly OcpiDbContext _context;

    public OcpiConnection? Connection { get; set; }
    public int CredentialLogsCount { get; set; }
    public DateTime? LastExchangeTime { get; set; }

    [BindProperty]
    public bool SimulateVersionsUnauthorized { get; set; }

    [BindProperty]
    public bool SimulateVersionsForbidden { get; set; }

    [BindProperty]
    public bool SimulateCredentialsUnauthorized { get; set; }

    [BindProperty]
    public bool SimulateCredentialsForbidden { get; set; }

    public DashboardModel(OcpiDbContext context)
    {
        _context = context;
    }

    public async Task OnGetAsync(int connectionId = 1)
    {
        Connection = await _context.OcpiConnections.FindAsync(connectionId);

        if (Connection != null)
        {
            SimulateVersionsUnauthorized = Connection.SimulateVersionsUnauthorized;
            SimulateVersionsForbidden = Connection.SimulateVersionsForbidden;
            SimulateCredentialsUnauthorized = Connection.SimulateCredentialsUnauthorized;
            SimulateCredentialsForbidden = Connection.SimulateCredentialsForbidden;

            var logs = _context.CredentialExchangeLogs
                .Where(l => l.OcpiConnectionId == connectionId)
                .OrderByDescending(l => l.Timestamp)
                .ToList();

            CredentialLogsCount = logs.Count;
            LastExchangeTime = logs.FirstOrDefault()?.Timestamp;
        }
    }

    public async Task<IActionResult> OnPostUpdateErrorSimulationAsync(int connectionId = 1)
    {
        var connection = await _context.OcpiConnections.FindAsync(connectionId);
        if (connection == null)
        {
            return NotFound();
        }

        connection.SimulateVersionsUnauthorized = SimulateVersionsUnauthorized;
        connection.SimulateVersionsForbidden = SimulateVersionsForbidden;
        connection.SimulateCredentialsUnauthorized = SimulateCredentialsUnauthorized;
        connection.SimulateCredentialsForbidden = SimulateCredentialsForbidden;
        connection.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return RedirectToPage(new { connectionId });
    }

    public string MaskToken(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return "(not set)";
        }

        if (token.Length <= 8)
        {
            return "***";
        }

        return token.Substring(0, 4) + "***" + token.Substring(token.Length - 4);
    }
}