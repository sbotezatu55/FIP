namespace Fip.Application.Abstractions.Persistence;

/// <summary>
/// Persistence projection used by flight read queries.
/// </summary>
public sealed record FlightQueryModel(
    Guid Id,
    string Icao24,
    string? Callsign,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    double? DepartureLatitude,
    double? DepartureLongitude,
    double? ArrivalLatitude,
    double? ArrivalLongitude,
    double? MaximumAltitudeFeet,
    int TelemetryPointCount,
    int EventCount);
