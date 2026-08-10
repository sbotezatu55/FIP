using Fip.Domain.Flights.Telemetry;

namespace Fip.Domain.Flights.Phases;

/// <summary>
/// Represents a contiguous interval classified as one flight phase.
/// </summary>
public sealed class FlightPhaseSegment
{
    public FlightPhaseSegment(
        FlightPhase phase,
        DateTimeOffset startTimestamp,
        DateTimeOffset endTimestamp,
        FlightTelemetryPoint? startPoint = null,
        FlightTelemetryPoint? endPoint = null)
    {
        if (endTimestamp < startTimestamp)
        {
            throw new ArgumentException("The phase segment must end at or after it starts.", nameof(endTimestamp));
        }

        Phase = phase;
        StartTimestamp = startTimestamp;
        EndTimestamp = endTimestamp;
        StartPoint = startPoint;
        EndPoint = endPoint;
    }

    public FlightPhase Phase { get; }

    public DateTimeOffset StartTimestamp { get; }

    public DateTimeOffset EndTimestamp { get; }

    public FlightTelemetryPoint? StartPoint { get; }

    public FlightTelemetryPoint? EndPoint { get; }

    public TimeSpan Duration => EndTimestamp - StartTimestamp;
}
