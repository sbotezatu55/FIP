namespace Fip.Domain.Flights.Telemetry;

public sealed class FlightTelemetryPoint
{
    public DateTimeOffset Timestamp { get; init; }

    public string Icao24 { get; init; } = string.Empty;

    public string? Callsign { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public double? AltitudeFeet { get; init; }

    public double? GroundSpeedKnots { get; init; }

    public double? TrackDegrees { get; init; }

    public double? VerticalRateFeetPerMinute { get; init; }
}
