using Fip.Application.Imports.ImportFlightTrajectory;

namespace Fip.Api.Models;

public sealed record ImportFlightTrajectoryResponse(
    ImportFlightTrajectoryStatus Status,
    Guid FlightId,
    string? Callsign,
    string Icao24,
    int PointsImported,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    int EventsDetected,
    IReadOnlyList<string> Warnings,
    ImportFlightDiagnosticsResponse Diagnostics)
{
    public static ImportFlightTrajectoryResponse FromResult(ImportFlightTrajectoryResult result) =>
        new(
            result.Status,
            result.FlightId,
            result.Callsign,
            result.Icao24,
            result.PointsImported,
            result.StartTime,
            result.EndTime,
            result.EventsDetected,
            result.Warnings,
            ImportFlightDiagnosticsResponse.FromDiagnostics(result.Diagnostics));
}

public sealed record ImportFlightDiagnosticsResponse(
    string Source,
    string Filename,
    DateTimeOffset ImportedAtUtc,
    int RecordsRead,
    int RecordsRejected,
    IReadOnlyList<string> Warnings,
    long DurationMilliseconds)
{
    public static ImportFlightDiagnosticsResponse FromDiagnostics(FlightImportDiagnostics diagnostics) =>
        new(
            diagnostics.Source,
            diagnostics.Filename,
            diagnostics.ImportedAtUtc,
            diagnostics.RecordsRead,
            diagnostics.RecordsRejected,
            diagnostics.Warnings,
            (long)Math.Round(diagnostics.Duration.TotalMilliseconds));
}
