using Fip.Application.Abstractions.Flights;
using Fip.Application.Abstractions.Persistence;
using Fip.Application.Abstractions.Telemetry;
using Fip.Application.Flights.Import.OpenSky;
using Fip.Application.Imports.ImportFlightTrajectory;
using Fip.Domain.FlightEvents;
using Fip.Domain.Flights;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Tests;

public sealed class ImportFlightTrajectoryServiceTests
{
    [Fact]
    public async Task ImportAsync_OrchestratesAndCommitsSuccessfulImport()
    {
        var importer = new FakeImporter(CreateSourcePoints(2));
        var validator = new FakeValidator();
        var reconstructor = new FakeReconstructor();
        var eventDetection = new FakeEventDetectionService();
        var summaryCalculator = new FakeSummaryCalculator();
        var repository = new FakeFlightRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(importer, validator, reconstructor, eventDetection, summaryCalculator, repository, unitOfWork);

        var result = await service.ImportAsync(new ImportFlightTrajectoryRequest("trajectory.json"));

        Assert.Equal(reconstructor.Flight.Id, result.FlightId);
        Assert.Equal(ImportFlightTrajectoryStatus.Imported, result.Status);
        Assert.Equal(reconstructor.Flight.Callsign, result.Callsign);
        Assert.Equal(reconstructor.Flight.Icao24, result.Icao24);
        Assert.Equal(reconstructor.Flight.TelemetryPoints.Count, result.PointsImported);
        Assert.Equal(reconstructor.Flight.StartTime, result.StartTime);
        Assert.Equal(reconstructor.Flight.EndTime, result.EndTime);
        Assert.Equal(reconstructor.Flight.Events.Count, result.EventsDetected);
        Assert.Empty(result.Warnings);
        Assert.Same(reconstructor.Flight, repository.AddedFlight);
        Assert.True(importer.WasCalled);
        Assert.True(validator.WasCalled);
        Assert.True(reconstructor.WasCalled);
        Assert.True(eventDetection.WasCalled);
        Assert.True(summaryCalculator.WasCalled);
        Assert.True(unitOfWork.WasCalled);
        Assert.Equal(1, result.EventsDetected);
        Assert.True(repository.DuplicateLookupWasCalled);
        Assert.Equal(reconstructor.Flight.Icao24, repository.LookupIcao24);
        Assert.Equal(reconstructor.Flight.StartTime, repository.LookupStartTime);
        Assert.Equal(reconstructor.Flight.EndTime, repository.LookupEndTime);
    }

    [Fact]
    public async Task ImportAsync_StreamOverloadUsesApplicationImportPipeline()
    {
        var importer = new FakeImporter(CreateSourcePoints(1));
        var service = CreateService(importer);
        await using var content = new MemoryStream([1, 2, 3]);

        var result = await service.ImportAsync("trajectory.json", content);

        Assert.Equal(ImportFlightTrajectoryStatus.Imported, result.Status);
        Assert.True(importer.WasCalled);
    }

    [Fact]
    public async Task ImportAsync_WhenMatchingFlightExists_ReturnsDuplicateWithoutDownstreamProcessing()
    {
        var existingFlightId = Guid.NewGuid();
        var eventDetection = new FakeEventDetectionService();
        var summaryCalculator = new FakeSummaryCalculator();
        var repository = new FakeFlightRepository { ExistingFlightId = existingFlightId };
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(
            eventDetection: eventDetection,
            summaryCalculator: summaryCalculator,
            repository: repository,
            unitOfWork: unitOfWork);

        var result = await service.ImportAsync(new ImportFlightTrajectoryRequest("trajectory.json"));

        Assert.Equal(ImportFlightTrajectoryStatus.Duplicate, result.Status);
        Assert.Equal(existingFlightId, result.FlightId);
        Assert.Equal(0, result.PointsImported);
        Assert.Equal(0, result.EventsDetected);
        Assert.Contains("matching flight already exists", result.Warnings.Single(warning => warning.Contains("matching flight")));
        Assert.Null(repository.AddedFlight);
        Assert.False(eventDetection.WasCalled);
        Assert.False(summaryCalculator.WasCalled);
        Assert.False(unitOfWork.WasCalled);
    }

    [Fact]
    public async Task ImportAsync_ExcludesInvalidPointsAndReturnsWarnings()
    {
        var importer = new FakeImporter(CreateSourcePoints(2));
        var validator = new FakeValidator(point => point.Latitude is < 0
            ? new TelemetryValidationResult(TelemetryValidationStatus.Invalid, [TelemetryValidationIssue.LatitudeOutOfRange])
            : new TelemetryValidationResult(TelemetryValidationStatus.Suspicious, [TelemetryValidationIssue.AltitudeUnusuallyHigh]));
        var repository = new FakeFlightRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(importer, validator, repository: repository, unitOfWork: unitOfWork);

        var result = await service.ImportAsync(new ImportFlightTrajectoryRequest("trajectory.json"));

        Assert.Equal(1, result.PointsImported);
        Assert.Contains("1 invalid telemetry point was excluded.", result.Warnings);
        Assert.Contains("1 suspicious telemetry point was retained.", result.Warnings);
        Assert.NotNull(repository.AddedFlight);
        Assert.True(unitOfWork.WasCalled);
    }

    [Fact]
    public async Task ImportAsync_WhenNoTelemetryIsUsable_DoesNotPersist()
    {
        var repository = new FakeFlightRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(
            new FakeImporter(CreateSourcePoints(2)),
            new FakeValidator(_ => new TelemetryValidationResult(TelemetryValidationStatus.Invalid, [TelemetryValidationIssue.InvalidTimestamp])),
            repository: repository,
            unitOfWork: unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ImportAsync(new ImportFlightTrajectoryRequest("trajectory.json")));

        Assert.Null(repository.AddedFlight);
        Assert.False(unitOfWork.WasCalled);
    }

    [Fact]
    public async Task ImportAsync_WhenReconstructionFails_DoesNotPersist()
    {
        var repository = new FakeFlightRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(
            new FakeImporter(CreateSourcePoints(1)),
            new FakeValidator(),
            new FakeReconstructor { Exception = new InvalidOperationException("cannot reconstruct") },
            repository: repository,
            unitOfWork: unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ImportAsync(new ImportFlightTrajectoryRequest("trajectory.json")));

        Assert.Null(repository.AddedFlight);
        Assert.False(unitOfWork.WasCalled);
    }

    [Fact]
    public async Task ImportAsync_WhenCallsignIsUnavailable_ReturnsNullCallsignWarning()
    {
        var service = CreateService(
            new FakeImporter(CreateSourcePoints(1)),
            reconstructor: new FakeReconstructor { Flight = CreateFlight(null) });

        var result = await service.ImportAsync(new ImportFlightTrajectoryRequest("trajectory.json"));

        Assert.Null(result.Callsign);
        Assert.Contains("No reliable callsign was available.", result.Warnings);
    }

    [Fact]
    public async Task ImportAsync_PropagatesCancellationTokenToAsyncCollaborators()
    {
        var cancellationToken = new CancellationTokenSource().Token;
        var importer = new FakeImporter(CreateSourcePoints(1));
        var repository = new FakeFlightRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(importer, repository: repository, unitOfWork: unitOfWork);

        await service.ImportAsync(new ImportFlightTrajectoryRequest("trajectory.json"), cancellationToken);

        Assert.Equal(cancellationToken, importer.CancellationToken);
        Assert.Equal(cancellationToken, repository.CancellationToken);
        Assert.Equal(cancellationToken, unitOfWork.CancellationToken);
    }

    [Fact]
    public async Task ImportAsync_WhenPersistenceFails_PropagatesFailure()
    {
        var repository = new FakeFlightRepository { Exception = new InvalidOperationException("persistence failed") };
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(repository: repository, unitOfWork: unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ImportAsync(new ImportFlightTrajectoryRequest("trajectory.json")));

        Assert.False(unitOfWork.WasCalled);
    }

    private static ImportFlightTrajectoryService CreateService(
        FakeImporter? importer = null,
        FakeValidator? validator = null,
        FakeReconstructor? reconstructor = null,
        FakeEventDetectionService? eventDetection = null,
        FakeSummaryCalculator? summaryCalculator = null,
        FakeFlightRepository? repository = null,
        FakeUnitOfWork? unitOfWork = null) =>
        new(
            importer ?? new FakeImporter(CreateSourcePoints(1)),
            validator ?? new FakeValidator(),
            reconstructor ?? new FakeReconstructor(),
            eventDetection ?? new FakeEventDetectionService(),
            summaryCalculator ?? new FakeSummaryCalculator(),
            repository ?? new FakeFlightRepository(),
            unitOfWork ?? new FakeUnitOfWork());

    private static IReadOnlyList<OpenSkyTelemetryPointDto> CreateSourcePoints(int count) =>
        Enumerable.Range(0, count)
            .Select(index => new OpenSkyTelemetryPointDto
            {
                Timestamp = 1_700_000_000_000 + index * 1_000,
                Icao24 = "abc123",
                Latitude = index == 1 ? -1 : 40 + index,
                Longitude = -73,
                Altitude = 10_000,
                GroundSpeed = 250
            })
            .ToList();

    private sealed class FakeImporter(IReadOnlyList<OpenSkyTelemetryPointDto> points) : IOpenSkyTrajectoryImporter
    {
        public bool WasCalled { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task<IReadOnlyList<OpenSkyTelemetryPointDto>> ImportAsync(Stream content, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            CancellationToken = cancellationToken;
            return Task.FromResult(points);
        }

        public Task<IReadOnlyList<OpenSkyTelemetryPointDto>> ImportAsync(string filePath, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            CancellationToken = cancellationToken;
            return Task.FromResult(points);
        }
    }

    private sealed class FakeValidator(Func<FlightTelemetryPoint, TelemetryValidationResult>? validate = null) : ITelemetryPointValidator
    {
        private readonly Func<FlightTelemetryPoint, TelemetryValidationResult> _validate = validate ?? (_ => new(TelemetryValidationStatus.Valid, []));
        public bool WasCalled { get; private set; }
        public TelemetryValidationResult Validate(FlightTelemetryPoint telemetryPoint) { WasCalled = true; return _validate(telemetryPoint); }
    }

    private sealed class FakeReconstructor : IFlightReconstructor
    {
        public Flight Flight { get; init; } = CreateFlight();
        public bool WasCalled { get; private set; }
        public Exception? Exception { get; init; }
        public Flight Reconstruct(IReadOnlyList<FlightTelemetryPoint> telemetryPoints) { WasCalled = true; if (Exception is not null) throw Exception; return Flight; }
    }

    private sealed class FakeEventDetectionService : IFlightEventDetectionService
    {
        public bool WasCalled { get; private set; }
        public IReadOnlyCollection<FlightEvent> Detect(IReadOnlyList<FlightTelemetryPoint> telemetryPoints) { WasCalled = true; return [new(FlightEventType.Takeoff, telemetryPoints[0].Timestamp)]; }
    }

    private sealed class FakeSummaryCalculator : IFlightSummaryCalculator
    {
        public FlightSummary Summary { get; } = new() { Duration = TimeSpan.FromMinutes(1) };
        public bool WasCalled { get; private set; }
        public FlightSummary Calculate(Flight flight) { WasCalled = true; return Summary; }
    }

    private sealed class FakeFlightRepository : IFlightRepository
    {
        public Flight? AddedFlight { get; private set; }
        public CancellationToken CancellationToken { get; private set; }
        public Exception? Exception { get; init; }
        public Guid? ExistingFlightId { get; init; }
        public bool DuplicateLookupWasCalled { get; private set; }
        public string? LookupIcao24 { get; private set; }
        public DateTimeOffset LookupStartTime { get; private set; }
        public DateTimeOffset LookupEndTime { get; private set; }
        public Task<Flight?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Flight?>(null);
        public Task<IReadOnlyList<FlightQueryModel>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FlightQueryModel>>([]);
        public Task<FlightQueryModel?> GetSummaryByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<FlightQueryModel?>(null);
        public Task<FlightTelemetryQueryResult> GetTelemetryAsync(Guid flightId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new FlightTelemetryQueryResult(false, []));
        public Task<FlightEventQueryResult> GetEventsAsync(Guid flightId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new FlightEventQueryResult(false, []));
        public Task<Guid?> FindExistingFlightIdAsync(
            string icao24,
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            CancellationToken cancellationToken = default)
        {
            DuplicateLookupWasCalled = true;
            CancellationToken = cancellationToken;
            LookupIcao24 = icao24;
            LookupStartTime = startTime;
            LookupEndTime = endTime;
            return Task.FromResult(ExistingFlightId);
        }

        public Task AddAsync(Flight flight, CancellationToken cancellationToken = default)
        {
            if (Exception is not null) throw Exception;
            AddedFlight = flight;
            CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public bool WasCalled { get; private set; }
        public CancellationToken CancellationToken { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { WasCalled = true; CancellationToken = cancellationToken; return Task.FromResult(1); }
    }

    private static Flight CreateFlight(string? callsign = "TEST") => new(
        "abc123", callsign, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(1),
        40, -73, 41, -73, 10_000,
        [new FlightTelemetryPoint { Timestamp = DateTimeOffset.UtcNow, Icao24 = "abc123", Latitude = 40, Longitude = -73 }]);
}
