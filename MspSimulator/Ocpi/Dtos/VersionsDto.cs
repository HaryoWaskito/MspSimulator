using System.Text.Json.Serialization;

namespace MspSimulator.Ocpi.Dtos;

// VERSION Dtos
public record OcpiVersionsResponse(
    [property: JsonPropertyName("status_code")] int StatusCode,
    [property: JsonPropertyName("status_message")] string? StatusMessage,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp,
    [property: JsonPropertyName("data")] List<OcpiVersionInfo> Data);

public record OcpiVersionInfo(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("url")] string Url);

// ENDPOINT Dtos
public record OcpiEndpointsResponse(
    [property: JsonPropertyName("status_code")] int StatusCode,
    [property: JsonPropertyName("status_message")] string? StatusMessage,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp,
    [property: JsonPropertyName("data")] OcpiEndpointData Data);

public record OcpiEndpointData(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("endpoints")] List<OcpiEndpointInfo> Endpoints
    );

public record OcpiEndpointInfo(
    [property: JsonPropertyName("identifier")] string Identifier,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("url")] string Url);
