using Fip.Application.Abstractions.Persistence;
using Fip.Application.Abstractions.Flights;
using Fip.Application.Flights;
using Fip.Domain.Flights;
using Fip.Domain.Flights.Telemetry;
using Fip.Domain.FlightEvents;
using Fip.SharedKernel.Geography;

namespace Fip.Application.Tests;

public sealed class FlightQueryServiceTests
{
    [Fact]
    public async Task GetFlightsAsync_WhenNoFlights_ReturnsEmptyCollection()
    {
        var service = CreateService(new FakeFlightRepository());

        var result = await service.GetFlightsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFlightsAsync_ReturnsMappedFlightsNewestFirst()
    {
        var first = CreateQueryModel(Guid.NewGuid(), "ABC123", "FIRST", DateTimeOffset.Parse("2026-08-09T13:00:00Z"), 12, 3);
        var second = CreateQueryModel(Guid.NewGuid(), "DEF456", null, DateTimeOffset.Parse("2026-08-09T12:00:00Z"), 8, 1);
        var repository = new FakeFlightRepository { Flights = [second, first] };
        var service = CreateService(repository);

        var result = await service.GetFlightsAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(first.Id, result[0].Id);
        Assert.Equal(first.Icao24, result[0].Icao24);
        Assert.Equal(first.Callsign, result[0].Callsign);
        Assert.Equal(first.DepartureLatitude, result[0].DepartureLatitude);
        Assert.Equal(first.EndTime - first.StartTime, result[0].Duration);
        Assert.Equal(first.TelemetryPointCount, result[0].TelemetryPointCount);
        Assert.Equal(first.EventCount, result[0].EventCount);
        Assert.Equal(second.Id, result[1].Id);
        Assert.Null(result[1].Callsign);
    }

    [Fact]
    public async Task GetFlightByIdAsync_ReturnsMappedFlight()
    {
        var model = CreateQueryModel(Guid.NewGuid(), "ABC123", "TRA051");
        var service = CreateService(new FakeFlightRepository { Flight = model });

        var result = await service.GetFlightByIdAsync(model.Id);

        Assert.NotNull(result);
        Assert.Equal(model.Id, result.Id);
        Assert.Equal(model.StartTime, result.StartTime);
        Assert.Equal(model.EndTime, result.EndTime);
        Assert.Equal(model.MaximumAltitudeFeet, result.MaximumAltitudeFeet);
        Assert.Equal(model.EndTime - model.StartTime, result.Duration);
    }

    [Fact]
    public async Task GetFlightByIdAsync_WhenUnknown_ReturnsNull()
    {
        var service = CreateService(new FakeFlightRepository());

        var result = await service.GetFlightByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetFlightSummaryAsync_UsesSummaryCalculatorAndMapsStatistics()
    {
        var flight = CreateSummaryFlight();
        var service = CreateService(new FakeFlightRepository { Aggregate = flight });

        var result = await service.GetFlightSummaryAsync(flight.Id);

        Assert.NotNull(result);
        Assert.Equal(flight.Id, result.FlightId);
        Assert.Equal(flight.Callsign, result.Callsign);
        Assert.Equal(flight.Icao24, result.Icao24);
        Assert.Equal(TimeSpan.FromMinutes(45), result.Duration);
        Assert.Equal(32_000, result.MaximumAltitudeFeet);
        Assert.Equal(250, result.MaximumGroundSpeedKnots);
        Assert.Equal(166.66666666666666, result.AverageGroundSpeedKnots);
        Assert.Equal(1_200, result.MaximumVerticalRateFeetPerMinute);
        Assert.Equal(-1_800, result.MinimumVerticalRateFeetPerMinute);
        Assert.InRange(result.DistanceTraveledNauticalMiles, 59.9, 60.1);
    }

    [Fact]
    public async Task GetFlightSummaryAsync_WhenUnknown_ReturnsNull()
    {
        var service = CreateService(new FakeFlightRepository());

        var result = await service.GetFlightSummaryAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetFlightTelemetryAsync_ReturnsChronologicalMappedPointsAndPreservesNulls()
    {
        var later = new FlightTelemetryQueryModel(
            DateTimeOffset.Parse("2026-08-09T12:01:00Z"),
            52.2,
            4.8,
            null,
            157,
            3.2,
            null);
        var earlier = new FlightTelemetryQueryModel(
            DateTimeOffset.Parse("2026-08-09T12:00:00Z"),
            52.1,
            4.7,
            224,
            155,
            3,
            2_240);
        var repository = new FakeFlightRepository
        {
            Telemetry = new FlightTelemetryQueryResult(true, [later, earlier])
        };
        var service = CreateService(repository);

        var result = await service.GetFlightTelemetryAsync(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(earlier.Timestamp, result[0].Timestamp);
        Assert.Equal(earlier.AltitudeFeet, result[0].AltitudeFeet);
        Assert.Equal(earlier.VerticalRateFeetPerMinute, result[0].VerticalRateFeetPerMinute);
        Assert.Null(result[1].AltitudeFeet);
        Assert.Null(result[1].VerticalRateFeetPerMinute);
    }

    [Fact]
    public async Task GetFlightTelemetryAsync_WhenFlightHasNoTelemetry_ReturnsEmptyCollection()
    {
        var service = CreateService(new FakeFlightRepository
        {
            Telemetry = new FlightTelemetryQueryResult(true, [])
        });

        var result = await service.GetFlightTelemetryAsync(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFlightTelemetryAsync_WhenUnknown_ReturnsNull()
    {
        var service = CreateService(new FakeFlightRepository
        {
            Telemetry = new FlightTelemetryQueryResult(false, [])
        });

        var result = await service.GetFlightTelemetryAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetFlightEventsAsync_ReturnsAllEventTypesChronologically()
    {
        var landingId = Guid.NewGuid();
        var takeoffId = Guid.NewGuid();
        var events = new FlightEventQueryResult(true,
        [
            new(landingId, Guid.NewGuid(), FlightEventType.Landing, DateTimeOffset.Parse("2026-08-09T13:00:00Z"), null, null, null, null),
            new(takeoffId, Guid.NewGuid(), FlightEventType.Takeoff, DateTimeOffset.Parse("2026-08-09T12:00:00Z"), 52.1, 4.7, 450, "Detected takeoff"),
            new(Guid.NewGuid(), Guid.NewGuid(), FlightEventType.TelemetryGap, DateTimeOffset.Parse("2026-08-09T12:30:00Z"), null, null, null, "Telemetry gap")
        ]);
        var service = CreateService(new FakeFlightRepository { Events = events });

        var result = await service.GetFlightEventsAsync(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("Takeoff", result[0].Type);
        Assert.Equal("TelemetryGap", result[1].Type);
        Assert.Equal("Landing", result[2].Type);
        Assert.Equal(52.1, result[0].Latitude);
        Assert.Equal(450, result[0].AltitudeFeet);
        Assert.Equal("Detected takeoff", result[0].Description);
    }

    [Fact]
    public async Task GetFlightEventsAsync_WhenFlightHasNoEvents_ReturnsEmptyCollection()
    {
        var service = CreateService(new FakeFlightRepository
        {
            Events = new FlightEventQueryResult(true, [])
        });

        var result = await service.GetFlightEventsAsync(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFlightEventsAsync_WhenUnknown_ReturnsNull()
    {
        var service = CreateService(new FakeFlightRepository
        {
            Events = new FlightEventQueryResult(false, [])
        });

        var result = await service.GetFlightEventsAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    private static FlightQueryService CreateService(FakeFlightRepository repository) =>
        new(repository, new FlightSummaryCalculator(new GeoDistanceCalculator()));

    private static Flight CreateSummaryFlight()
    {
        var startTime = DateTimeOffset.Parse("2026-08-09T12:00:00Z");
        var points = new[]
        {
            new FlightTelemetryPoint
            {
                Timestamp = startTime,
                Icao24 = "abc123",
                Latitude = 0,
                Longitude = 0,
                AltitudeFeet = 10_000,
                GroundSpeedKnots = 100,
                VerticalRateFeetPerMinute = 500
            },
            new FlightTelemetryPoint
            {
                Timestamp = startTime.AddMinutes(15),
                Icao24 = "abc123",
                Latitude = 1,
                Longitude = 0,
                AltitudeFeet = 32_000,
                GroundSpeedKnots = 250,
                VerticalRateFeetPerMinute = 1_200
            },
            new FlightTelemetryPoint
            {
                Timestamp = startTime.AddMinutes(30),
                Icao24 = "abc123",
                Latitude = 1,
                Longitude = 0,
                AltitudeFeet = 25_000,
                GroundSpeedKnots = 150,
                VerticalRateFeetPerMinute = -1_800
            }
        };

        return new Flight(
            "abc123",
            "TRA051",
            startTime,
            startTime.AddMinutes(45),
            0,
            0,
            1,
            0,
            32_000,
            points);
    }

    private static FlightQueryModel CreateQueryModel(
        Guid id,
        string icao24,
        string? callsign,
        DateTimeOffset? startTime = null,
        int telemetryPointCount = 4,
        int eventCount = 2) => new(
        id,
        icao24,
        callsign,
        startTime ?? DateTimeOffset.Parse("2026-08-09T12:00:00Z"),
        (startTime ?? DateTimeOffset.Parse("2026-08-09T12:00:00Z")).AddHours(1),
        52.1,
        4.2,
        51.5,
        0.1,
        35_000,
        telemetryPointCount,
        eventCount);

    private sealed class FakeFlightRepository : IFlightRepository
    {
        public IReadOnlyList<FlightQueryModel> Flights { get; init; } = [];
        public FlightQueryModel? Flight { get; init; }
        public Flight? Aggregate { get; init; }
        public FlightTelemetryQueryResult Telemetry { get; init; } = new(false, []);
        public FlightEventQueryResult Events { get; init; } = new(false, []);

        public Task<IReadOnlyList<FlightQueryModel>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Flights);

        public Task<FlightQueryModel?> GetSummaryByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Flight?.Id == id ? Flight : null);

        public Task<Flight?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Aggregate?.Id == id ? Aggregate : null);

        public Task<FlightTelemetryQueryResult> GetTelemetryAsync(
            Guid flightId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Telemetry);

        public Task<FlightEventQueryResult> GetEventsAsync(
            Guid flightId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Events);

        public Task<Guid?> FindExistingFlightIdAsync(
            string icao24,
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(null);

        public Task AddAsync(Flight flight, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }
}
