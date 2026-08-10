using Fip.Domain.FlightEvents;
using Fip.Domain.Flights;
using Fip.Domain.Flights.Telemetry;
using Fip.Persistence.Entities;
using Fip.Persistence.Repositories;

namespace Fip.Infrastructure.Tests;

public sealed class FlightMapperTests
{
    [Fact]
    public void ToEntity_PreservesFlightTelemetryAndEventState()
    {
        var flight = CreateFlight();

        var entity = FlightMapper.ToEntity(flight);

        Assert.Equal(flight.Id, entity.Id);
        Assert.Equal(flight.Icao24, entity.Icao24);
        Assert.Equal(flight.Callsign, entity.Callsign);
        Assert.Equal(flight.StartTime, entity.StartTime);
        Assert.Equal(flight.EndTime, entity.EndTime);
        Assert.Equal(flight.TelemetryPoints.Count, entity.TelemetryPoints.Count);
        Assert.Equal(flight.Events.Count, entity.Events.Count);
        Assert.All(entity.TelemetryPoints, point => Assert.Equal(flight.Id, point.FlightId));
        Assert.All(entity.Events, flightEvent => Assert.Equal(flight.Id, flightEvent.FlightId));

        var telemetryPoint = Assert.Single(entity.TelemetryPoints);
        Assert.Equal(52.1, telemetryPoint.Latitude);
        Assert.Equal(4.2, telemetryPoint.Longitude);
        Assert.Equal(240, telemetryPoint.GroundSpeedKnots);

        var eventEntity = Assert.Single(entity.Events);
        Assert.Equal(FlightEventType.Takeoff, eventEntity.Type);
        Assert.Equal(52.1, eventEntity.Latitude);
        Assert.Equal("Detected takeoff", eventEntity.Description);
    }

    [Fact]
    public void ToDomain_RestoresIdentityAndChronologicalChildOrdering()
    {
        var flightId = Guid.NewGuid();
        var later = DateTimeOffset.Parse("2026-08-09T12:05:00Z");
        var earlier = later.AddMinutes(-5);
        var entity = new FlightEntity
        {
            Id = flightId,
            Icao24 = "484506",
            Callsign = "TRA051",
            StartTime = earlier,
            EndTime = later,
            TelemetryPoints = new List<FlightTelemetryPointEntity>
            {
                new() { Timestamp = later, Icao24 = "484506" },
                new() { Timestamp = earlier, Icao24 = "484506" }
            },
            Events = new List<FlightEventEntity>
            {
                new() { Type = FlightEventType.Landing, Timestamp = later },
                new() { Type = FlightEventType.Takeoff, Timestamp = earlier }
            }
        };

        var flight = FlightMapper.ToDomain(entity);

        Assert.Equal(flightId, flight.Id);
        Assert.Equal(new[] { earlier, later }, flight.TelemetryPoints.Select(point => point.Timestamp));
        Assert.Equal(
            new[] { FlightEventType.Takeoff, FlightEventType.Landing },
            flight.Events.Select(flightEvent => flightEvent.Type));
    }

    private static Flight CreateFlight()
    {
        var timestamp = DateTimeOffset.Parse("2026-08-09T12:00:00Z");
        var telemetryPoint = new FlightTelemetryPoint
        {
            Timestamp = timestamp,
            Icao24 = "484506",
            Callsign = "TRA051",
            Latitude = 52.1,
            Longitude = 4.2,
            AltitudeFeet = 1_000,
            GroundSpeedKnots = 240,
            TrackDegrees = 90,
            VerticalRateFeetPerMinute = 500
        };

        var flight = new Flight(
            "484506",
            "TRA051",
            timestamp,
            timestamp.AddMinutes(30),
            52.1,
            4.2,
            51.5,
            0.1,
            35_000,
            new[] { telemetryPoint });

        flight.AddEvent(new FlightEvent(
            FlightEventType.Takeoff,
            timestamp,
            telemetryPoint,
            "Detected takeoff"));

        return flight;
    }
}
