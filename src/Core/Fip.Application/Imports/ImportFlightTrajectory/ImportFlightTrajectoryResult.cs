namespace Fip.Application.Imports.ImportFlightTrajectory;

public enum ImportFlightTrajectoryStatus
{
    Imported,
    Duplicate
}

public sealed record ImportFlightTrajectoryResult(
    ImportFlightTrajectoryStatus Status,
    Guid FlightId,
    string? Callsign,
    string Icao24,
    int PointsImported,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    int EventsDetected,
    IReadOnlyList<string> Warnings);
