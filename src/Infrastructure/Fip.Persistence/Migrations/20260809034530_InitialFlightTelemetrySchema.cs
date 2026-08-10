using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fip.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialFlightTelemetrySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Flights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Icao24 = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Callsign = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    StartTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DepartureLatitude = table.Column<double>(type: "float", nullable: true),
                    DepartureLongitude = table.Column<double>(type: "float", nullable: true),
                    ArrivalLatitude = table.Column<double>(type: "float", nullable: true),
                    ArrivalLongitude = table.Column<double>(type: "float", nullable: true),
                    MaximumAltitudeFeet = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FlightEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlightId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    AltitudeFeet = table.Column<double>(type: "float", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlightEvents_Flights_FlightId",
                        column: x => x.FlightId,
                        principalTable: "Flights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlightTelemetryPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlightId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Icao24 = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Callsign = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    AltitudeFeet = table.Column<double>(type: "float", nullable: true),
                    GroundSpeedKnots = table.Column<double>(type: "float", nullable: true),
                    TrackDegrees = table.Column<double>(type: "float", nullable: true),
                    VerticalRateFeetPerMinute = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightTelemetryPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlightTelemetryPoints_Flights_FlightId",
                        column: x => x.FlightId,
                        principalTable: "Flights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlightEvents_FlightId_Timestamp",
                table: "FlightEvents",
                columns: new[] { "FlightId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Flights_Callsign",
                table: "Flights",
                column: "Callsign");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_EndTime",
                table: "Flights",
                column: "EndTime");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_Icao24",
                table: "Flights",
                column: "Icao24");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_StartTime",
                table: "Flights",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_FlightTelemetryPoints_FlightId_Timestamp",
                table: "FlightTelemetryPoints",
                columns: new[] { "FlightId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlightEvents");

            migrationBuilder.DropTable(
                name: "FlightTelemetryPoints");

            migrationBuilder.DropTable(
                name: "Flights");
        }
    }
}
