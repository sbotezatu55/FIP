using Fip.Application.Abstractions.Flights;
using Fip.Application.Abstractions.Persistence;
using Fip.Application.Abstractions.Telemetry;
using Fip.Application.Flights;
using Fip.Application.Imports.ImportFlightTrajectory;
using Fip.Application.Telemetry;
using Fip.Infrastructure.Flights.Import.OpenSky;
using Fip.Domain.FlightEvents;
using Fip.Domain.Flights;
using Fip.SharedKernel.Geography;

namespace Fip.Application.Tests.Integration.Imports;

public sealed class Tra051ImportIntegrationTests
{
    [Fact]
    public async Task ImportAsync_ProcessesRealTra051TrajectoryEndToEnd()
    {
        var samplePath = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "TRA051_B738_2018-05-30.json");
        Assert.True(File.Exists(samplePath), $"The TRA051 integration fixture was not copied to '{samplePath}'.");
        var captureRepository = new CapturingFlightRepository();
        ITelemetryPointValidator validator = new TelemetryPointValidator();
        var detectors = new IFlightEventDetector[]
        {
            new TakeoffDetector(validator),
            new LandingDetector(validator),
            new TopOfClimbDetector(),
            new TopOfDescentDetector()
        };
        IImportFlightTrajectoryService importService = new ImportFlightTrajectoryService(
            new OpenSkyJsonTrajectoryImporter(),
            validator,
            new FlightReconstructor(),
            new FlightEventDetectionService(detectors),
            new FlightSummaryCalculator(new GeoDistanceCalculator()),
            captureRepository,
            new NoOpUnitOfWork());
        var result = await importService.ImportAsync(new ImportFlightTrajectoryRequest(samplePath));
        var flight = captureRepository.AddedFlight;
        var diagnostics = $"points={result.PointsImported}, callsign={result.Callsign}, icao24={result.Icao24}, " +
                          $"start={result.StartTime:O}, end={result.EndTime:O}, " +
                          $"maximumAltitudeFeet={flight?.MaximumAltitudeFeet}, " +
                          $"events={string.Join(",", flight?.Events.Select(flightEvent => flightEvent.Type) ?? [])}";

        Assert.Equal(ImportFlightTrajectoryStatus.Imported, result.Status);
        Assert.True(result.PointsImported > 0, diagnostics);
        Assert.Equal("OpenSky", result.Diagnostics.Source);
        Assert.Equal("TRA051_B738_2018-05-30.json", result.Diagnostics.Filename);
        Assert.True(result.Diagnostics.ImportedAtUtc <= DateTimeOffset.UtcNow, diagnostics);
        Assert.True(result.Diagnostics.RecordsRead > 0, diagnostics);
        Assert.True(result.Diagnostics.RecordsRejected >= 0, diagnostics);
        Assert.True(result.Diagnostics.Duration > TimeSpan.Zero, diagnostics);
        Assert.NotEqual(Guid.Empty, result.FlightId);
        Assert.NotNull(flight);
        Assert.False(string.IsNullOrWhiteSpace(flight!.Icao24), diagnostics);
        Assert.True(flight.Callsign?.Contains("TRA051", StringComparison.OrdinalIgnoreCase) == true, diagnostics);
        Assert.True(flight.StartTime < flight.EndTime, diagnostics);
        Assert.True(flight.EndTime - flight.StartTime > TimeSpan.Zero, diagnostics);
        Assert.True(flight.MaximumAltitudeFeet > 10_000, diagnostics);
        Assert.All(flight.TelemetryPoints, point =>
        {
            Assert.True(point.Latitude is >= -90 and <= 90 && double.IsFinite(point.Latitude.Value), diagnostics);
            Assert.True(point.Longitude is >= -180 and <= 180 && double.IsFinite(point.Longitude.Value), diagnostics);
        });

        Assert.Contains(flight.Events, flightEvent => flightEvent.Type == FlightEventType.Takeoff);
        Assert.Contains(flight.Events, flightEvent => flightEvent.Type == FlightEventType.Landing);
        Assert.All(flight.Events, flightEvent =>
            Assert.InRange(flightEvent.Timestamp, flight.StartTime, flight.EndTime));
    }

    private sealed class CapturingFlightRepository : IFlightRepository
    {
        public Flight? AddedFlight { get; private set; }

        public Task<IReadOnlyList<FlightQueryModel>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FlightQueryModel>>([]);

        public Task<FlightQueryModel?> GetSummaryByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<FlightQueryModel?>(null);

        public Task<FlightTelemetryQueryResult> GetTelemetryAsync(Guid flightId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new FlightTelemetryQueryResult(false, []));

        public Task<FlightEventQueryResult> GetEventsAsync(Guid flightId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new FlightEventQueryResult(false, []));

        public Task<Flight?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Flight?>(AddedFlight);

        public Task<Guid?> FindExistingFlightIdAsync(
            string icao24,
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(null);

        public Task AddAsync(Flight flight, CancellationToken cancellationToken = default)
        {
            AddedFlight = flight;
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(1);
    }
}
