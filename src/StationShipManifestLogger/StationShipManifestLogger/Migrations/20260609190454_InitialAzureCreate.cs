using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StationShipManifestLogger.Migrations
{
    /// <inheritdoc />
    public partial class InitialAzureCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManifestAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Callsign = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShipName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CaptainName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoggedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManifestAuditLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManifestAuditLogs");
        }
    }
}
