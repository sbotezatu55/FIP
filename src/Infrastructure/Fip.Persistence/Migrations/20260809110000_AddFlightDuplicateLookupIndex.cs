using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fip.Persistence.Migrations;

/// <inheritdoc />
[Migration("20260809110000_AddFlightDuplicateLookupIndex")]
public partial class AddFlightDuplicateLookupIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Flights_Icao24_StartTime_EndTime",
            table: "Flights",
            columns: new[] { "Icao24", "StartTime", "EndTime" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Flights_Icao24_StartTime_EndTime",
            table: "Flights");
    }
}
