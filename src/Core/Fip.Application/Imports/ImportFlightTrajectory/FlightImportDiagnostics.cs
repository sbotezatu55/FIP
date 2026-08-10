namespace Fip.Application.Imports.ImportFlightTrajectory;

/// <summary>
/// Describes how a flight trajectory was ingested, independently of the flight domain model.
/// </summary>
public sealed class FlightImportDiagnostics
{
    public string Source { get; init; } = string.Empty;

    public string Filename { get; init; } = string.Empty;

    public DateTimeOffset ImportedAtUtc { get; init; }

    public int RecordsRead { get; init; }

    public int RecordsRejected { get; init; }

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public TimeSpan Duration { get; init; }
}
