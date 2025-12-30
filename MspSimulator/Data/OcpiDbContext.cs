using Microsoft.EntityFrameworkCore;
using MspSimulator.Data.Entities;

namespace MspSimulator.Data;

public class OcpiDbContext : DbContext
{
    public OcpiDbContext(DbContextOptions<OcpiDbContext> options)
        : base(options)
    {
    }

    public DbSet<OcpiConnection> OcpiConnections { get; set; } = null!;

    public DbSet<CredentialExchangeLog> CredentialExchangeLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureOcpiConnection(modelBuilder);
        ConfigureCredentialExchangeLog(modelBuilder);
    }

    private static void ConfigureOcpiConnection(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OcpiConnection>(entity =>
        {
            entity.ToTable("OcpiConnections");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.CpoPartyId)
                .IsRequired()
                .HasMaxLength(3);

            entity.Property(e => e.CpoCountryCode)
                .IsRequired()
                .HasMaxLength(2);

            entity.Property(e => e.BaseUrl)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.OcpiToken)
                .HasMaxLength(500);

            entity.Property(e => e.ClientToken)
                .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("datetime('now')");

            entity.Property(e => e.UpdatedAt);

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.HandshakeMode)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("None");

            entity.Property(e => e.SimulateVersionsUnauthorized)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.SimulateVersionsForbidden)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.SimulateCredentialsUnauthorized)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.SimulateCredentialsForbidden)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.RawVersionsPayload)
                .HasColumnType("TEXT");

            entity.Property(e => e.RawCredentialsPayload)
                .HasColumnType("TEXT");
        });
    }

    private static void ConfigureCredentialExchangeLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CredentialExchangeLog>(entity =>
        {
            entity.ToTable("CredentialExchangeLogs");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.OcpiConnectionId)
                .IsRequired();

            entity.Property(e => e.Direction)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(e => e.Method)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(e => e.Endpoint)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.HttpStatusCode)
                .IsRequired();

            entity.Property(e => e.RequestPayload)
                .HasColumnType("TEXT");

            entity.Property(e => e.ResponsePayload)
                .HasColumnType("TEXT");

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(500);

            entity.Property(e => e.Timestamp)
                .IsRequired()
                .HasDefaultValueSql("datetime('now')");
        });
    }
}
