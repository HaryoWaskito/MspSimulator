namespace MspSimulator.Services;

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
