namespace Fip.Application.Flights;

public sealed record FlightListItemDto(
    Guid Id,
    string Icao24,
    string? Callsign,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    TimeSpan Duration,
    double? MaximumAltitudeFeet,
    double? DepartureLatitude,
    double? DepartureLongitude,
    double? ArrivalLatitude,
    double? ArrivalLongitude,
    int TelemetryPointCount,
    int EventCount);
