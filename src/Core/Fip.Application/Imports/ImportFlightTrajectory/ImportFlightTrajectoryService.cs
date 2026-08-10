using Fip.Application.Abstractions.Flights;
using Fip.Application.Abstractions.Persistence;
using Fip.Application.Abstractions.Telemetry;
using Fip.Application.Flights.Import.OpenSky;
using Fip.Domain.Flights.Telemetry;

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
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("An uploaded trajectory file name is required.", nameof(fileName));
        }

        ArgumentNullException.ThrowIfNull(content);

        var sourcePoints = await importer.ImportAsync(content, cancellationToken);

        return await ImportPointsAsync(sourcePoints, cancellationToken);
    }

    public async Task<ImportFlightTrajectoryResult> ImportAsync(
        ImportFlightTrajectoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.FilePath))
        {
            throw new ArgumentException("A trajectory file path is required.", nameof(request));
        }

        var sourcePoints = await importer.ImportAsync(request.FilePath, cancellationToken);

        return await ImportPointsAsync(sourcePoints, cancellationToken);
    }

    private async Task<ImportFlightTrajectoryResult> ImportPointsAsync(
        IReadOnlyList<OpenSkyTelemetryPointDto> sourcePoints,
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
            var duplicateWarnings = BuildWarnings(invalidPointCount, suspiciousPointCount, flight.Callsign)
                .ToList();
            duplicateWarnings.Add("Flight was not imported because a matching flight already exists.");

            return new ImportFlightTrajectoryResult(
                ImportFlightTrajectoryStatus.Duplicate,
                existingFlightId.Value,
                flight.Callsign,
                flight.Icao24,
                0,
                flight.StartTime,
                flight.EndTime,
                0,
                duplicateWarnings.AsReadOnly());
        }

        var events = flightEventDetectionService.Detect(flight.TelemetryPoints);

        foreach (var flightEvent in events.OrderBy(flightEvent => flightEvent.Timestamp))
        {
            flight.AddEvent(flightEvent);
        }

        _ = flightSummaryCalculator.Calculate(flight);
        await flightRepository.AddAsync(flight, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var warnings = BuildWarnings(invalidPointCount, suspiciousPointCount, flight.Callsign);

        return new ImportFlightTrajectoryResult(
            ImportFlightTrajectoryStatus.Imported,
            flight.Id,
            flight.Callsign,
            flight.Icao24,
            flight.TelemetryPoints.Count,
            flight.StartTime,
            flight.EndTime,
            flight.Events.Count,
            warnings);
    }

    private static IReadOnlyList<string> BuildWarnings(
        int invalidPointCount,
        int suspiciousPointCount,
        string? callsign)
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

        return warnings.Count == 0 ? Array.Empty<string>() : warnings.AsReadOnly();
    }

    private static string GetVerb(int count) => count == 1 ? "was" : "were";
}
