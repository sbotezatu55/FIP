namespace Fip.Application.Flights;

public sealed record FlightTelemetryPointDto(
    DateTimeOffset Timestamp,
    double? Latitude,
    double? Longitude,
    double? AltitudeFeet,
    double? GroundSpeedKnots,
    double? TrackDegrees,
    double? VerticalRateFeetPerMinute);
