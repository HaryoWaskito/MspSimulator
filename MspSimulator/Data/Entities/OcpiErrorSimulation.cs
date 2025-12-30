namespace MspSimulator.Data.Entities;

public class OcpiErrorSimulation
{
    public int Id { get; set; }

    public bool ForceUnauthorized { get; set; }

    public bool ForceForbidden { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
