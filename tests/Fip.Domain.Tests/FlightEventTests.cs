using Fip.Domain.FlightEvents;
using Fip.Domain.Flights;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Domain.Tests;

public sealed class FlightEventTests
{
    [Fact]
    public void Constructor_PreservesEventValues()
    {
        var timestamp = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var telemetryPoint = new FlightTelemetryPoint
        {
            Timestamp = timestamp,
            Icao24 = "abc123"
        };

        var flightEvent = new FlightEvent(
            FlightEventType.Takeoff,
            timestamp,
            telemetryPoint,
            "Takeoff detected");

        Assert.Equal(FlightEventType.Takeoff, flightEvent.Type);
        Assert.Equal(timestamp, flightEvent.Timestamp);
        Assert.Same(telemetryPoint, flightEvent.TelemetryPoint);
        Assert.Equal("Takeoff detected", flightEvent.Description);
    }

    [Fact]
    public void Flight_AddEvent_ExposesReadOnlyEventCollection()
    {
        var flight = CreateFlight();
        var flightEvent = new FlightEvent(FlightEventType.TelemetryGap, DateTimeOffset.UtcNow);

        flight.AddEvent(flightEvent);

        var collection = Assert.IsAssignableFrom<IList<FlightEvent>>(flight.Events);
        Assert.Single(flight.Events);
        Assert.Same(flightEvent, flight.Events[0]);
        Assert.Throws<NotSupportedException>(() => collection.Add(
            new FlightEvent(FlightEventType.Landing, DateTimeOffset.UtcNow)));
        Assert.Single(flight.Events);
    }

    [Fact]
    public void Flight_AddEvent_RejectsNull()
    {
        var flight = CreateFlight();

        Assert.Throws<ArgumentNullException>(() => flight.AddEvent(null!));
    }

    private static Flight CreateFlight()
    {
        var timestamp = DateTimeOffset.UtcNow;

        return new Flight(
            "abc123",
            null,
            timestamp,
            timestamp,
            null,
            null,
            null,
            null,
            null,
            Array.Empty<FlightTelemetryPoint>());
    }
}
