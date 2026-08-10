namespace Fip.Persistence.Entities;

/// <summary>
/// Persistence representation of a reconstructed flight.
/// </summary>
public sealed class FlightEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Icao24 { get; set; } = string.Empty;

    public string? Callsign { get; set; }

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }

    public double? DepartureLatitude { get; set; }

    public double? DepartureLongitude { get; set; }

    public double? ArrivalLatitude { get; set; }

    public double? ArrivalLongitude { get; set; }

    public double? MaximumAltitudeFeet { get; set; }

    public ICollection<FlightTelemetryPointEntity> TelemetryPoints { get; set; } = new List<FlightTelemetryPointEntity>();

    public ICollection<FlightEventEntity> Events { get; set; } = new List<FlightEventEntity>();
}
