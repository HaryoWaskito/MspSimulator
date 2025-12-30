namespace MspSimulator.Data.Entities;

public class CredentialExchangeLog
{
    public int Id { get; set; }

    public int OcpiConnectionId { get; set; }

    public string Direction { get; set; } = null!;

    public string Method { get; set; } = null!;

    public string Endpoint { get; set; } = null!;

    public int HttpStatusCode { get; set; }

    public string? RequestPayload { get; set; }

    public string? ResponsePayload { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime Timestamp { get; set; }
}
