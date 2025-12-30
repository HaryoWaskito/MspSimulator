using System.Text.Json.Serialization;

namespace MspSimulator.Ocpi.Dtos;

public record OcpiCredential(
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("hub_party_id")] string? HubPartyId = null,
    [property: JsonPropertyName("roles")] OcpiRole[]? Roles = null
);

public record OcpiRole(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("party_id")] string PartyId,
    [property: JsonPropertyName("country_code")] string CountryCode,
    [property: JsonPropertyName("business_details")] OcpiBusinessDetails BusinessDetails
);

public record OcpiBusinessDetails(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("logo")] OcpiImage? Logo,
    [property: JsonPropertyName("website")] string? Website
);

public record OcpiImage(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("thumbnail")] string? Thumbnail,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height
);

public record OcpiCredentialsResponse(
    [property: JsonPropertyName("status_code")] string StatusCode,
    [property: JsonPropertyName("status_message")] string? StatusMessage,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp,
    [property: JsonPropertyName("data")] OcpiCredential? Data
);
