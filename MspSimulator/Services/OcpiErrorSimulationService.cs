using Microsoft.EntityFrameworkCore;
using MspSimulator.Data;
using MspSimulator.Data.Entities;

namespace MspSimulator.Services;

public class OcpiErrorSimulationService : IOcpiErrorSimulationService
{
    private readonly OcpiDbContext _context;
    private readonly ILogger<OcpiErrorSimulationService> _logger;

    public OcpiErrorSimulationService(OcpiDbContext context, ILogger<OcpiErrorSimulationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> IsUnauthorizedForcedAsync()
    {
        var simulation = await GetOrCreateSimulationAsync();
        return simulation.ForceUnauthorized;
    }

    public async Task<bool> IsForbiddenForcedAsync()
    {
        var simulation = await GetOrCreateSimulationAsync();
        return simulation.ForceForbidden;
    }

    public async Task SetUnauthorizedAsync(bool force)
    {
        var simulation = await GetOrCreateSimulationAsync();
        simulation.ForceUnauthorized = force;
        simulation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Global ForceUnauthorized set to {Value}", force);
    }

    public async Task SetForbiddenAsync(bool force)
    {
        var simulation = await GetOrCreateSimulationAsync();
        simulation.ForceForbidden = force;
        simulation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Global ForceForbidden set to {Value}", force);
    }

    public async Task SetBothAsync(bool forceUnauthorized, bool forceForbidden)
    {
        var simulation = await GetOrCreateSimulationAsync();
        simulation.ForceUnauthorized = forceUnauthorized;
        simulation.ForceForbidden = forceForbidden;
        simulation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Global error simulation updated: ForceUnauthorized={Unauthorized}, ForceForbidden={Forbidden}",
            forceUnauthorized,
            forceForbidden);
    }

    public async Task<(bool ForceUnauthorized, bool ForceForbidden)> GetCurrentSettingsAsync()
    {
        var simulation = await GetOrCreateSimulationAsync();
        return (simulation.ForceUnauthorized, simulation.ForceForbidden);
    }

    public async Task ResetAsync()
    {
        var simulation = await GetOrCreateSimulationAsync();
        simulation.ForceUnauthorized = false;
        simulation.ForceForbidden = false;
        simulation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Global error simulation reset to defaults");
    }

    private async Task<OcpiErrorSimulation> GetOrCreateSimulationAsync()
    {
        var simulation = await _context.ErrorSimulations.FirstOrDefaultAsync();

        if (simulation == null)
        {
            simulation = new OcpiErrorSimulation
            {
                ForceUnauthorized = false,
                ForceForbidden = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.ErrorSimulations.Add(simulation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created global error simulation configuration");
        }

        return simulation;
    }
}
