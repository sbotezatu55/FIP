namespace Fip.Persistence.Entities;

/// <summary>
/// Persistence representation of one normalized telemetry point.
/// </summary>
public sealed class FlightTelemetryPointEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FlightId { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public string Icao24 { get; set; } = string.Empty;

    public string? Callsign { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public double? AltitudeFeet { get; set; }

    public double? GroundSpeedKnots { get; set; }

    public double? TrackDegrees { get; set; }

    public double? VerticalRateFeetPerMinute { get; set; }

    public FlightEntity Flight { get; set; } = null!;
}
