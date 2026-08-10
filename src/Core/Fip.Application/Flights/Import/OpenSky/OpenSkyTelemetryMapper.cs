using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Flights.Import.OpenSky;

public static class OpenSkyTelemetryMapper
{
    public static FlightTelemetryPoint Map(OpenSkyTelemetryPointDto source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new FlightTelemetryPoint
        {
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(source.Timestamp),
            Icao24 = source.Icao24,
            Callsign = source.Callsign?.Trim(),
            Latitude = source.Latitude,
            Longitude = source.Longitude,
            AltitudeFeet = source.Altitude,
            GroundSpeedKnots = source.GroundSpeed,
            TrackDegrees = source.Track,
            VerticalRateFeetPerMinute = source.VerticalRate
        };
    }

    public static IReadOnlyList<FlightTelemetryPoint> Map(
        IEnumerable<OpenSkyTelemetryPointDto> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Select(Map).ToList();
    }
}
