namespace Fip.Application.Flights;

public sealed record FlightEventDto(
    Guid Id,
    Guid FlightId,
    string Type,
    DateTimeOffset Timestamp,
    double? Latitude,
    double? Longitude,
    double? AltitudeFeet,
    string? Description);
