using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MspSimulator.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CredentialExchangeLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OcpiConnectionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Direction = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Method = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Endpoint = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    HttpStatusCode = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestPayload = table.Column<string>(type: "TEXT", nullable: true),
                    ResponsePayload = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CredentialExchangeLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OcpiConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CpoPartyId = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    CpoCountryCode = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    BaseUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    OcpiToken = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ClientToken = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    HandshakeMode = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "None"),
                    SimulateVersionsUnauthorized = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    SimulateVersionsForbidden = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    RawVersionsPayload = table.Column<string>(type: "TEXT", nullable: true),
                    RawCredentialsPayload = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OcpiConnections", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CredentialExchangeLogs");

            migrationBuilder.DropTable(
                name: "OcpiConnections");
        }
    }
}
