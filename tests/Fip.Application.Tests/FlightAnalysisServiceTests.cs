using Fip.Application.Abstractions.Flights;
using Fip.Application.Abstractions.Persistence;
using Fip.Application.Flights;
using Fip.Domain.Flights;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Tests;

public sealed class FlightAnalysisServiceTests
{
    [Fact]
    public async Task RecalculateAsync_replaces_events_from_persisted_telemetry()
    {
        var flight = CreateFlight();
        var repository = new FakeFlightRepository(flight);
        var analysisRepository = new FakeFlightAnalysisRepository();
        var unitOfWork = new FakeUnitOfWork();
        var expectedEvent = new Fip.Domain.FlightEvents.FlightEvent(
            Fip.Domain.FlightEvents.FlightEventType.Takeoff,
            flight.StartTime);
        var detector = new FakeEventDetectionService(expectedEvent);
        var service = new FlightAnalysisService(repository, analysisRepository, unitOfWork, detector);

        var result = await service.RecalculateAsync(flight.Id);

        Assert.NotNull(result);
        Assert.Equal(1, result.EventsDetected);
        Assert.Equal(new[] { expectedEvent }, analysisRepository.Events);
        Assert.Equal(1, unitOfWork.SaveCalls);
        Assert.Same(flight, repository.LoadedFlight);
    }

    [Fact]
    public async Task RecalculateAsync_returns_null_for_unknown_flight()
    {
        var repository = new FakeFlightRepository(null);
        var service = new FlightAnalysisService(repository, new FakeFlightAnalysisRepository(), new FakeUnitOfWork(), new FakeEventDetectionService());

        var result = await service.RecalculateAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    private static Flight CreateFlight()
    {
        var timestamp = DateTimeOffset.Parse("2026-08-09T12:00:00Z");
        return new Flight(
            "abc123",
            "TEST01",
            timestamp,
            timestamp.AddHours(1),
            52,
            4,
            51,
            5,
            30_000,
            new[] { new FlightTelemetryPoint { Timestamp = timestamp, Icao24 = "abc123" } });
    }

    private sealed class FakeEventDetectionService(params Fip.Domain.FlightEvents.FlightEvent[] events) : IFlightEventDetectionService
    {
        public IReadOnlyCollection<Fip.Domain.FlightEvents.FlightEvent> Detect(IReadOnlyList<FlightTelemetryPoint> telemetryPoints) => events;
    }

    private sealed class FakeFlightAnalysisRepository : IFlightAnalysisRepository
    {
        public IReadOnlyCollection<Fip.Domain.FlightEvents.FlightEvent> Events { get; private set; } = [];

        public Task<bool> ReplaceEventsAsync(Guid flightId, IReadOnlyCollection<Fip.Domain.FlightEvents.FlightEvent> events, CancellationToken cancellationToken = default)
        {
            Events = events;
            return Task.FromResult(true);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeFlightRepository(Flight? flight) : IFlightRepository
    {
        public Flight? LoadedFlight { get; private set; }

        public Task<IReadOnlyList<FlightQueryModel>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<FlightQueryModel>>([]);
        public Task<FlightQueryModel?> GetSummaryByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<FlightQueryModel?>(null);
        public Task<FlightTelemetryQueryResult> GetTelemetryAsync(Guid flightId, CancellationToken cancellationToken = default) => Task.FromResult(new FlightTelemetryQueryResult(false, []));
        public Task<FlightEventQueryResult> GetEventsAsync(Guid flightId, CancellationToken cancellationToken = default) => Task.FromResult(new FlightEventQueryResult(false, []));
        public Task<Flight?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            LoadedFlight = flight?.Id == id ? flight : null;
            return Task.FromResult(LoadedFlight);
        }
        public Task<Guid?> FindExistingFlightIdAsync(string icao24, DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(null);
        public Task AddAsync(Flight flight, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }
}
