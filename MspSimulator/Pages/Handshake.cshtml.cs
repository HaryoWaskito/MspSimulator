using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MspSimulator.Data;
using MspSimulator.Data.Entities;
using MspSimulator.Services;

namespace MspSimulator.Pages;

public class HandshakeModel : PageModel
{
    private readonly OcpiDbContext _context;
    private readonly IOcpiHandshakeService _handshakeService;
    private readonly ILogger<HandshakeModel> _logger;

    public OcpiConnection? Connection { get; set; }
    public HandshakeResult? LastResult { get; set; }
    public string? ResultMessage { get; set; }
    public bool ShowResult { get; set; }

    public HandshakeModel(
        OcpiDbContext context,
        IOcpiHandshakeService handshakeService,
        ILogger<HandshakeModel> logger)
    {
        _context = context;
        _handshakeService = handshakeService;
        _logger = logger;
    }

    public async Task OnGetAsync(int connectionId = 1)
    {
        Connection = await _context.OcpiConnections.FindAsync(connectionId);
    }

    public async Task<IActionResult> OnPostInitiateHandshakeAsync(int connectionId = 1)
    {
        Connection = await _context.OcpiConnections.FindAsync(connectionId);
        if (Connection == null)
        {
            ResultMessage = "Connection not found";
            ShowResult = true;
            return Page();
        }

        _logger.LogInformation("User triggered EMSP-initiated handshake for connection {Id}", connectionId);

        LastResult = await _handshakeService.InitiateEmspHandshakeAsync(connectionId);
        ShowResult = true;

        // Refresh connection state
        Connection = await _context.OcpiConnections.FindAsync(connectionId);

        return Page();
    }

    public async Task<IActionResult> OnPostRevokeAsync(int connectionId = 1)
    {
        Connection = await _context.OcpiConnections.FindAsync(connectionId);
        if (Connection == null)
        {
            ResultMessage = "Connection not found";
            ShowResult = true;
            return Page();
        }

        _logger.LogInformation("User triggered credential revocation for connection {Id}", connectionId);

        LastResult = await _handshakeService.RevokeCredentialsAsync(connectionId);
        ShowResult = true;

        // Refresh connection state
        Connection = await _context.OcpiConnections.FindAsync(connectionId);

        return Page();
    }
}
