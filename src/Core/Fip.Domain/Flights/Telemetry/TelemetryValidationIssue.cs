namespace Fip.Domain.Flights.Telemetry;

public enum TelemetryValidationIssue
{
    LatitudeOutOfRange,
    LongitudeOutOfRange,
    TrackOutOfRange,
    InvalidTimestamp,
    AltitudeUnusuallyHigh,
    GroundSpeedUnusuallyHigh,
    VerticalRateUnusuallyHigh
}
