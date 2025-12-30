namespace MspSimulator.Ocpi.Client.Dtos;

public record OcpiClientResponse<T>(int StatusCode, T? Data, string? RawPayload);
