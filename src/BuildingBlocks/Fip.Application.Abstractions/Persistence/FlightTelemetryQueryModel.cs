namespace Fip.Application.Abstractions.Persistence;

public sealed record FlightTelemetryQueryModel(
    DateTimeOffset Timestamp,
    double? Latitude,
    double? Longitude,
    double? AltitudeFeet,
    double? GroundSpeedKnots,
    double? TrackDegrees,
    double? VerticalRateFeetPerMinute);

public sealed record FlightTelemetryQueryResult(
    bool FlightExists,
    IReadOnlyList<FlightTelemetryQueryModel> Points);
