using Fip.Domain.Flights.Telemetry;

namespace Fip.Domain.FlightEvents;

/// <summary>
/// Represents an event detected for a reconstructed flight.
/// </summary>
public sealed class FlightEvent
{
    public FlightEvent(
        FlightEventType type,
        DateTimeOffset timestamp,
        FlightTelemetryPoint? telemetryPoint = null,
        string? description = null)
    {
        Type = type;
        Timestamp = timestamp;
        TelemetryPoint = telemetryPoint;
        Description = description;
    }

    public FlightEventType Type { get; }

    public DateTimeOffset Timestamp { get; }

    public FlightTelemetryPoint? TelemetryPoint { get; }

    public string? Description { get; }
}
