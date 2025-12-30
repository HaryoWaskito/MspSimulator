using MspSimulator.Ocpi.Dtos;

namespace MspSimulator.Services;

public interface IOcpiVersionsService
{
    Task<(int StatusCode, OcpiVersionsResponse Response)> GetVersionsAsync(int connectionId);
}
