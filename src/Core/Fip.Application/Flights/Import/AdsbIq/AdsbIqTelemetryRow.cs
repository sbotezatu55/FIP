namespace Fip.Application.Flights.Import.AdsbIq;

public sealed class AdsbIqTelemetryRow
{
    public DateTimeOffset Timestamp { get; init; }
    public string Icao24 { get; init; } = string.Empty;
    public string? Callsign { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public double? BarometricAltitudeFeet { get; init; }
    public double? GeometricAltitudeFeet { get; init; }
    public double? GroundSpeedKnots { get; init; }
    public double? TrackDegrees { get; init; }
    public double? BarometricRateFeetPerMinute { get; init; }
    public double? GeometricRateFeetPerMinute { get; init; }
    public string? Squawk { get; init; }
    public string? EmitterCategory { get; init; }
    public bool IsRemoved { get; init; }
}
