using Fip.Application.Abstractions.Flights;
using Fip.Application.Abstractions.Persistence;
using Fip.Application.Abstractions.Telemetry;
using Fip.Application.Flights.Import.AdsbIq;
using Fip.Application.Imports.ImportFlightTrajectory;
using Fip.Domain.FlightEvents;
using Fip.Domain.Flights;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Imports.ImportFlightPreview;

public sealed class ImportFlightPreviewService(
    IAdsbIqTelemetryImporter importer,
    IFlightReconstructor reconstructor,
    ITelemetryPointValidator validator,
    IFlightEventDetectionService eventDetection,
    IFlightSummaryCalculator summaryCalculator,
    IFlightRepository repository,
    IUnitOfWork unitOfWork,
    IImportFlightPreviewStore store) : IImportFlightPreviewService
{
    private static readonly TimeSpan MaximumFlightGap = TimeSpan.FromMinutes(5);

    public async Task<ImportFlightPreviewResult> PreviewAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        var rows = await importer.ImportAsync(content, cancellationToken);
        var deduplicated = rows.Where(row => !row.IsRemoved).GroupBy(row => (row.Icao24, row.Timestamp)).Select(group => group.First()).ToList();
        var candidates = new List<ImportFlightCandidate>();

        foreach (var aircraft in deduplicated.GroupBy(row => row.Icao24, StringComparer.Ordinal))
        {
            var points = aircraft.OrderBy(row => row.Timestamp).Select(row => new FlightTelemetryPoint
            {
                Timestamp = row.Timestamp,
                Icao24 = row.Icao24,
                Callsign = Clean(row.Callsign, row.Icao24),
                Latitude = row.Latitude,
                Longitude = row.Longitude,
                AltitudeFeet = row.BarometricAltitudeFeet ?? row.GeometricAltitudeFeet,
                GroundSpeedKnots = row.GroundSpeedKnots,
                TrackDegrees = row.TrackDegrees,
                VerticalRateFeetPerMinute = row.BarometricRateFeetPerMinute ?? row.GeometricRateFeetPerMinute
            }).Where(point => validator.Validate(point).Status != TelemetryValidationStatus.Invalid).ToList();

            if (points.Count == 0) continue;

            foreach (var segment in SplitIntoFlightSegments(points))
            {
                if (segment.Count == 0) continue;
                var flight = reconstructor.Reconstruct(segment);
                var status = DetermineStatus(flight, rows.Min(row => row.Timestamp), rows.Max(row => row.Timestamp));
                candidates.Add(new ImportFlightCandidate(Guid.NewGuid(), flight.Callsign, flight.Icao24, flight.StartTime, flight.EndTime, flight.TelemetryPoints.Count, status, flight));
            }
        }

        var previewId = Guid.NewGuid();
        store.Add(previewId, candidates.ToDictionary(candidate => candidate.CandidateId));
        return new ImportFlightPreviewResult(previewId, candidates, "ADSBiq", Path.GetFileName(fileName), rows.Count, rows.Count - deduplicated.Count);
    }

    public Task<ImportFlightCandidate?> GetCandidateAsync(Guid previewId, Guid candidateId, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Get(previewId, candidateId));

    public async Task<ImportFlightTrajectoryResult?> ImportCandidateAsync(Guid previewId, Guid candidateId, CancellationToken cancellationToken = default)
    {
        var candidate = await GetCandidateAsync(previewId, candidateId, cancellationToken);
        if (candidate is null) return null;
        var existing = await repository.FindExistingFlightIdAsync(candidate.Icao24, candidate.StartTime, candidate.EndTime, cancellationToken);
        if (existing is not null) return new ImportFlightTrajectoryResult(ImportFlightTrajectoryStatus.Duplicate, existing.Value, candidate.Callsign, candidate.Icao24, 0, candidate.StartTime, candidate.EndTime, 0, ["Flight was already imported."], new FlightImportDiagnostics { Source = "ADSBiq", Filename = "preview", ImportedAtUtc = DateTimeOffset.UtcNow });
        foreach (var item in eventDetection.Detect(candidate.Flight.TelemetryPoints).OrderBy(item => item.Timestamp)) candidate.Flight.AddEvent(item);
        _ = summaryCalculator.Calculate(candidate.Flight);
        await repository.AddAsync(candidate.Flight, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        store.Remove(previewId, candidateId);
        return new ImportFlightTrajectoryResult(ImportFlightTrajectoryStatus.Imported, candidate.Flight.Id, candidate.Callsign, candidate.Icao24, candidate.Points, candidate.StartTime, candidate.EndTime, candidate.Flight.Events.Count, [], new FlightImportDiagnostics { Source = "ADSBiq", Filename = "preview", ImportedAtUtc = DateTimeOffset.UtcNow });
    }

    public bool IgnoreCandidate(Guid previewId, Guid candidateId) => store.Remove(previewId, candidateId);

    private static ImportFlightCandidateStatus DetermineStatus(Flight flight, DateTimeOffset sourceStart, DateTimeOffset sourceEnd)
    {
        if (flight.TelemetryPoints.Count < 10 || flight.EndTime - flight.StartTime < TimeSpan.FromMinutes(2)) return ImportFlightCandidateStatus.TooShort;
        // A candidate whose bounds equal the file bounds has no evidence of being truncated.
        // The previous comparison classified every single-flight download as PartialStart
        // because a zero-minute difference also satisfies the two-minute threshold.
        var startsAtBoundary = sourceStart < flight.StartTime && flight.StartTime - sourceStart < TimeSpan.FromMinutes(2);
        var endsAtBoundary = sourceEnd > flight.EndTime && sourceEnd - flight.EndTime < TimeSpan.FromMinutes(2);
        if (startsAtBoundary) return ImportFlightCandidateStatus.PartialStart;
        if (endsAtBoundary) return ImportFlightCandidateStatus.PartialEnd;
        return ImportFlightCandidateStatus.Complete;
    }

    private static IReadOnlyList<IReadOnlyList<FlightTelemetryPoint>> SplitIntoFlightSegments(
        IReadOnlyList<FlightTelemetryPoint> points)
    {
        var segments = new List<IReadOnlyList<FlightTelemetryPoint>>();
        var current = new List<FlightTelemetryPoint>();

        foreach (var point in points.OrderBy(point => point.Timestamp))
        {
            if (current.Count > 0 && point.Timestamp - current[^1].Timestamp > MaximumFlightGap)
            {
                segments.Add(current);
                current = new List<FlightTelemetryPoint>();
            }

            current.Add(point);
        }

        if (current.Count > 0) segments.Add(current);
        return segments;
    }

    private static string? Clean(string? value, string icao24) =>
        string.IsNullOrWhiteSpace(value) || string.Equals(value.Trim(), icao24, StringComparison.OrdinalIgnoreCase)
            ? null
            : value.Trim();
}
