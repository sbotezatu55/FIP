namespace Fip.Domain.Flights.Telemetry;

/// <summary>
/// Represents the elapsed time between consecutive telemetry observations that exceeds the expected interval.
/// </summary>
public sealed record TelemetryGap(
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    TimeSpan Duration);
