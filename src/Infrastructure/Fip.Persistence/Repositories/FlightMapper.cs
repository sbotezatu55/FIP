using Fip.Domain.FlightEvents;
using Fip.Domain.Flights;
using Fip.Domain.Flights.Telemetry;
using Fip.Persistence.Entities;

namespace Fip.Persistence.Repositories;

internal static class FlightMapper
{
    public static FlightEntity ToEntity(Flight flight)
    {
        ArgumentNullException.ThrowIfNull(flight);

        var entity = new FlightEntity
        {
            Id = flight.Id,
            Icao24 = flight.Icao24,
            Callsign = flight.Callsign,
            StartTime = flight.StartTime,
            EndTime = flight.EndTime,
            DepartureLatitude = flight.DepartureLatitude,
            DepartureLongitude = flight.DepartureLongitude,
            ArrivalLatitude = flight.ArrivalLatitude,
            ArrivalLongitude = flight.ArrivalLongitude,
            MaximumAltitudeFeet = flight.MaximumAltitudeFeet
        };

        entity.TelemetryPoints = flight.TelemetryPoints
            .Select(point => new FlightTelemetryPointEntity
            {
                FlightId = entity.Id,
                Timestamp = point.Timestamp,
                Icao24 = point.Icao24,
                Callsign = point.Callsign,
                Latitude = point.Latitude,
                Longitude = point.Longitude,
                AltitudeFeet = point.AltitudeFeet,
                GroundSpeedKnots = point.GroundSpeedKnots,
                TrackDegrees = point.TrackDegrees,
                VerticalRateFeetPerMinute = point.VerticalRateFeetPerMinute
            })
            .ToList();

        entity.Events = flight.Events
            .Select(flightEvent => new FlightEventEntity
            {
                FlightId = entity.Id,
                Type = flightEvent.Type,
                Timestamp = flightEvent.Timestamp,
                Latitude = flightEvent.TelemetryPoint?.Latitude,
                Longitude = flightEvent.TelemetryPoint?.Longitude,
                AltitudeFeet = flightEvent.TelemetryPoint?.AltitudeFeet,
                Description = flightEvent.Description
            })
            .ToList();

        return entity;
    }

    public static Flight ToDomain(FlightEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var telemetryPoints = entity.TelemetryPoints
            .OrderBy(point => point.Timestamp)
            .Select(point => new FlightTelemetryPoint
            {
                Timestamp = point.Timestamp,
                Icao24 = point.Icao24,
                Callsign = point.Callsign,
                Latitude = point.Latitude,
                Longitude = point.Longitude,
                AltitudeFeet = point.AltitudeFeet,
                GroundSpeedKnots = point.GroundSpeedKnots,
                TrackDegrees = point.TrackDegrees,
                VerticalRateFeetPerMinute = point.VerticalRateFeetPerMinute
            })
            .ToList();

        var flight = Flight.Reconstitute(
            entity.Id,
            entity.Icao24,
            entity.Callsign,
            entity.StartTime,
            entity.EndTime,
            entity.DepartureLatitude,
            entity.DepartureLongitude,
            entity.ArrivalLatitude,
            entity.ArrivalLongitude,
            entity.MaximumAltitudeFeet,
            telemetryPoints);

        foreach (var eventEntity in entity.Events.OrderBy(flightEvent => flightEvent.Timestamp))
        {
            flight.AddEvent(ToDomainEvent(eventEntity, entity));
        }

        return flight;
    }

    private static FlightEvent ToDomainEvent(FlightEventEntity entity, FlightEntity flight)
    {
        FlightTelemetryPoint? telemetryPoint = null;

        if (entity.Latitude.HasValue || entity.Longitude.HasValue || entity.AltitudeFeet.HasValue)
        {
            telemetryPoint = new FlightTelemetryPoint
            {
                Timestamp = entity.Timestamp,
                Icao24 = flight.Icao24,
                Callsign = flight.Callsign,
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                AltitudeFeet = entity.AltitudeFeet
            };
        }

        return new FlightEvent(entity.Type, entity.Timestamp, telemetryPoint, entity.Description);
    }
}
