using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MspSimulator.Migrations
{
    /// <inheritdoc />
    public partial class _2ndIteration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SimulateCredentialsForbidden",
                table: "OcpiConnections",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SimulateCredentialsUnauthorized",
                table: "OcpiConnections",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SimulateCredentialsForbidden",
                table: "OcpiConnections");

            migrationBuilder.DropColumn(
                name: "SimulateCredentialsUnauthorized",
                table: "OcpiConnections");
        }
    }
}
