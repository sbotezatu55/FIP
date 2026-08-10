using Fip.Application.Abstractions.Flights;
using Fip.Application.Abstractions.Persistence;
using Fip.Application.Abstractions.Telemetry;
using Fip.Application.Flights.Import.OpenSky;
using Fip.Domain.FlightEvents;
using Fip.Domain.Flights.Telemetry;
using System.Diagnostics;

namespace Fip.Application.Imports.ImportFlightTrajectory;

public sealed class ImportFlightTrajectoryService(
    IOpenSkyTrajectoryImporter importer,
    ITelemetryPointValidator telemetryPointValidator,
    IFlightReconstructor flightReconstructor,
    IFlightEventDetectionService flightEventDetectionService,
    IFlightSummaryCalculator flightSummaryCalculator,
    IFlightRepository flightRepository,
    IUnitOfWork unitOfWork) : IImportFlightTrajectoryService
{
    public async Task<ImportFlightTrajectoryResult> ImportAsync(
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var importedAtUtc = DateTimeOffset.UtcNow;

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("An uploaded trajectory file name is required.", nameof(fileName));
        }

        ArgumentNullException.ThrowIfNull(content);

        var sourcePoints = await importer.ImportAsync(content, cancellationToken);

        return await ImportPointsAsync(
            sourcePoints,
            Path.GetFileName(fileName),
            importedAtUtc,
            stopwatch,
            cancellationToken);
    }

    public async Task<ImportFlightTrajectoryResult> ImportAsync(
        ImportFlightTrajectoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var importedAtUtc = DateTimeOffset.UtcNow;

        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.FilePath))
        {
            throw new ArgumentException("A trajectory file path is required.", nameof(request));
        }

        var sourcePoints = await importer.ImportAsync(request.FilePath, cancellationToken);

        return await ImportPointsAsync(
            sourcePoints,
            Path.GetFileName(request.FilePath),
            importedAtUtc,
            stopwatch,
            cancellationToken);
    }

    private async Task<ImportFlightTrajectoryResult> ImportPointsAsync(
        IReadOnlyList<OpenSkyTelemetryPointDto> sourcePoints,
        string fileName,
        DateTimeOffset importedAtUtc,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sourcePoints);

        if (sourcePoints.Count == 0)
        {
            throw new InvalidOperationException("The trajectory file contains no telemetry records.");
        }

        var normalizedPoints = OpenSkyTelemetryMapper.Map(sourcePoints);
        var usablePoints = new List<FlightTelemetryPoint>(normalizedPoints.Count);
        var invalidPointCount = 0;
        var suspiciousPointCount = 0;

        foreach (var point in normalizedPoints)
        {
            var validation = telemetryPointValidator.Validate(point);

            if (validation.Status == TelemetryValidationStatus.Invalid)
            {
                invalidPointCount++;
            }
            else
            {
                usablePoints.Add(point);

                if (validation.Status == TelemetryValidationStatus.Suspicious)
                {
                    suspiciousPointCount++;
                }
            }
        }

        if (usablePoints.Count == 0)
        {
            throw new InvalidOperationException("The trajectory contains no usable telemetry after validation.");
        }

        var flight = flightReconstructor.Reconstruct(usablePoints);
        var existingFlightId = await flightRepository.FindExistingFlightIdAsync(
            flight.Icao24,
            flight.StartTime,
            flight.EndTime,
            cancellationToken);

        if (existingFlightId is not null)
        {
            var duplicateWarnings = BuildWarnings(
                    invalidPointCount,
                    suspiciousPointCount,
                    flight.Callsign,
                    Array.Empty<FlightEvent>())
                .ToList();
            duplicateWarnings.Add("Flight was not imported because a matching flight already exists.");
            stopwatch.Stop();

            return new ImportFlightTrajectoryResult(
                ImportFlightTrajectoryStatus.Duplicate,
                existingFlightId.Value,
                flight.Callsign,
                flight.Icao24,
                0,
                flight.StartTime,
                flight.EndTime,
                0,
                duplicateWarnings.AsReadOnly(),
                CreateDiagnostics(
                    fileName,
                    importedAtUtc,
                    sourcePoints.Count,
                    invalidPointCount,
                    duplicateWarnings,
                    stopwatch));
        }

        var events = flightEventDetectionService.Detect(flight.TelemetryPoints);

        foreach (var flightEvent in events.OrderBy(flightEvent => flightEvent.Timestamp))
        {
            flight.AddEvent(flightEvent);
        }

        _ = flightSummaryCalculator.Calculate(flight);
        await flightRepository.AddAsync(flight, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var warnings = BuildWarnings(
            invalidPointCount,
            suspiciousPointCount,
            flight.Callsign,
            events);
        stopwatch.Stop();

        return new ImportFlightTrajectoryResult(
            ImportFlightTrajectoryStatus.Imported,
            flight.Id,
            flight.Callsign,
            flight.Icao24,
            flight.TelemetryPoints.Count,
            flight.StartTime,
            flight.EndTime,
            flight.Events.Count,
            warnings,
            CreateDiagnostics(
                fileName,
                importedAtUtc,
                sourcePoints.Count,
                invalidPointCount,
                warnings,
                stopwatch));
    }

    private static IReadOnlyList<string> BuildWarnings(
        int invalidPointCount,
        int suspiciousPointCount,
        string? callsign,
        IReadOnlyCollection<FlightEvent> events)
    {
        var warnings = new List<string>(capacity: 3);

        if (invalidPointCount > 0)
        {
            warnings.Add($"{invalidPointCount} invalid telemetry point{(invalidPointCount == 1 ? "" : "s")} {GetVerb(invalidPointCount)} excluded.");
        }

        if (suspiciousPointCount > 0)
        {
            warnings.Add($"{suspiciousPointCount} suspicious telemetry point{(suspiciousPointCount == 1 ? "" : "s")} {GetVerb(suspiciousPointCount)} retained.");
        }

        if (string.IsNullOrWhiteSpace(callsign))
        {
            warnings.Add("No reliable callsign was available.");
        }

        var telemetryGapCount = events.Count(flightEvent => flightEvent.Type == FlightEventType.TelemetryGap);

        if (telemetryGapCount > 0)
        {
            warnings.Add($"{telemetryGapCount} telemetry gap{(telemetryGapCount == 1 ? "" : "s")} detected.");
        }

        return warnings.Count == 0 ? Array.Empty<string>() : warnings.AsReadOnly();
    }

    private static FlightImportDiagnostics CreateDiagnostics(
        string fileName,
        DateTimeOffset importedAtUtc,
        int recordsRead,
        int recordsRejected,
        IReadOnlyList<string> warnings,
        Stopwatch stopwatch) =>
        new()
        {
            Source = "OpenSky",
            Filename = fileName,
            ImportedAtUtc = importedAtUtc,
            RecordsRead = recordsRead,
            RecordsRejected = recordsRejected,
            Warnings = warnings,
            Duration = stopwatch.Elapsed
        };

    private static string GetVerb(int count) => count == 1 ? "was" : "were";
}
