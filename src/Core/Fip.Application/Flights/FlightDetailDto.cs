namespace Fip.Application.Flights;

public sealed record FlightDetailDto(
    Guid Id,
    string Icao24,
    string? Callsign,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    TimeSpan Duration,
    double? DepartureLatitude,
    double? DepartureLongitude,
    double? ArrivalLatitude,
    double? ArrivalLongitude,
    double? MaximumAltitudeFeet);
