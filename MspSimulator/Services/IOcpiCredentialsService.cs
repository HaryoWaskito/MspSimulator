using MspSimulator.Ocpi.Dtos;

namespace MspSimulator.Services;

public interface IOcpiCredentialsService
{
    Task<(int StatusCode, OcpiCredentialsResponse Response)> PostCredentialsAsync(
        int connectionId,
        OcpiCredential request,
        string rawPayload);

    Task<(int StatusCode, OcpiCredentialsResponse Response)> DeleteCredentialsAsync(int connectionId);
}
