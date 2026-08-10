using Fip.Application.Abstractions.Flights;
using Fip.Application.Flights;
using Fip.Domain.FlightEvents;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Tests;

public sealed class FlightEventDetectionServiceTests
{
    private static readonly DateTimeOffset BaseTimestamp =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Detect_ReturnsEmptyWhenNoDetectorsProduceEvents()
    {
        var service = new FlightEventDetectionService(new[]
        {
            new StubDetector()
        });

        var result = service.Detect(CreateTelemetry());

        Assert.Empty(result);
    }

    [Fact]
    public void Detect_ReturnsEventFromOneDetector()
    {
        var expectedEvent = CreateEvent(FlightEventType.Takeoff, 10);
        var service = new FlightEventDetectionService(new[]
        {
            new StubDetector(expectedEvent)
        });

        var result = service.Detect(CreateTelemetry());

        var actualEvent = Assert.Single(result);
        Assert.Same(expectedEvent, actualEvent);
    }

    [Fact]
    public void Detect_CombinesEventsFromMultipleDetectors()
    {
        var takeoff = CreateEvent(FlightEventType.Takeoff, 10);
        var landing = CreateEvent(FlightEventType.Landing, 20);
        var service = new FlightEventDetectionService(new[]
        {
            new StubDetector(takeoff),
            new StubDetector(landing)
        });

        var result = service.Detect(CreateTelemetry());

        Assert.Equal(new[] { takeoff, landing }, result);
    }

    [Fact]
    public void Detect_SortsCombinedEventsChronologically()
    {
        var lateEvent = CreateEvent(FlightEventType.Landing, 30);
        var earlyEvent = CreateEvent(FlightEventType.Takeoff, 10);
        var service = new FlightEventDetectionService(new[]
        {
            new StubDetector(lateEvent),
            new StubDetector(earlyEvent)
        });

        var result = service.Detect(CreateTelemetry());

        Assert.Equal(new[] { earlyEvent, lateEvent }, result);
    }

    [Fact]
    public void Detect_RetainsMultipleEventsFromOneDetector()
    {
        var firstEvent = CreateEvent(FlightEventType.Takeoff, 10);
        var secondEvent = CreateEvent(FlightEventType.TelemetryGap, 20);
        var service = new FlightEventDetectionService(new[]
        {
            new StubDetector(firstEvent, secondEvent)
        });

        var result = service.Detect(CreateTelemetry());

        Assert.Equal(new[] { firstEvent, secondEvent }, result);
    }

    [Fact]
    public void Detect_ReturnsEmptyForEmptyTelemetryWithoutInvokingDetectors()
    {
        var detector = new StubDetector(CreateEvent(FlightEventType.Takeoff, 10));
        var service = new FlightEventDetectionService(new[] { detector });

        var result = service.Detect(Array.Empty<FlightTelemetryPoint>());

        Assert.Empty(result);
        Assert.False(detector.WasInvoked);
    }

    [Fact]
    public void Detect_ReturnsReadOnlyResults()
    {
        var flightEvent = CreateEvent(FlightEventType.Takeoff, 10);
        var service = new FlightEventDetectionService(new[]
        {
            new StubDetector(flightEvent)
        });

        var result = service.Detect(CreateTelemetry());
        var collection = Assert.IsAssignableFrom<IList<FlightEvent>>(result);

        Assert.Throws<NotSupportedException>(() => collection.Add(CreateEvent(FlightEventType.Landing, 20)));
    }

    private static IReadOnlyList<FlightTelemetryPoint> CreateTelemetry() => new[]
    {
        new FlightTelemetryPoint
        {
            Timestamp = BaseTimestamp,
            Icao24 = "abc123"
        }
    };

    private static FlightEvent CreateEvent(FlightEventType type, int seconds) =>
        new(type, BaseTimestamp.AddSeconds(seconds));

    private sealed class StubDetector : IFlightEventDetector
    {
        private readonly IReadOnlyCollection<FlightEvent> _events;

        public StubDetector(params FlightEvent[] events)
        {
            _events = events;
        }

        public bool WasInvoked { get; private set; }

        public IReadOnlyCollection<FlightEvent> Detect(IReadOnlyList<FlightTelemetryPoint> telemetryPoints)
        {
            WasInvoked = true;
            return _events;
        }
    }
}
