namespace Fip.Application.Flights;

public sealed record FlightSummaryDto(
    Guid FlightId,
    string? Callsign,
    string Icao24,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    TimeSpan Duration,
    double? MaximumAltitudeFeet,
    double? MaximumGroundSpeedKnots,
    double? AverageGroundSpeedKnots,
    double? MaximumVerticalRateFeetPerMinute,
    double? MinimumVerticalRateFeetPerMinute,
    double DistanceTraveledNauticalMiles,
    DateTimeOffset? TakeoffTime,
    DateTimeOffset? LandingTime,
    TimeSpan? FlightTime);
