using Fip.Domain.FlightEvents;

namespace Fip.Persistence.Entities;

/// <summary>
/// Persistence representation of a detected event in a reconstructed flight.
/// </summary>
public sealed class FlightEventEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FlightId { get; set; }

    public FlightEventType Type { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public double? AltitudeFeet { get; set; }

    public string? Description { get; set; }

    public FlightEntity Flight { get; set; } = null!;
}
