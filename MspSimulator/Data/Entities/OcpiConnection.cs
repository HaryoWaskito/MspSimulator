namespace MspSimulator.Data.Entities;

public class OcpiConnection
{
    public int Id { get; set; }

    public string CpoPartyId { get; set; } = null!;

    public string CpoCountryCode { get; set; } = null!;

    public string BaseUrl { get; set; } = null!;

    public string? OcpiToken { get; set; }

    public string? ClientToken { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; }

    public int HandshakeMode { get; set; }

    public string Status { get; set; } = "None";

    public bool SimulateVersionsUnauthorized { get; set; }

    public bool SimulateVersionsForbidden { get; set; }

    public bool SimulateCredentialsUnauthorized { get; set; }

    public bool SimulateCredentialsForbidden { get; set; }

    public string? RawVersionsPayload { get; set; }

    public string? RawCredentialsPayload { get; set; }
}
