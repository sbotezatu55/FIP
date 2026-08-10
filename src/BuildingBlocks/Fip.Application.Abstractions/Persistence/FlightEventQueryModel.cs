using Fip.Domain.FlightEvents;

namespace Fip.Application.Abstractions.Persistence;

public sealed record FlightEventQueryModel(
    Guid Id,
    Guid FlightId,
    FlightEventType Type,
    DateTimeOffset Timestamp,
    double? Latitude,
    double? Longitude,
    double? AltitudeFeet,
    string? Description);

public sealed record FlightEventQueryResult(
    bool FlightExists,
    IReadOnlyList<FlightEventQueryModel> Events);
