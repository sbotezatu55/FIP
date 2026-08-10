using Fip.Application.Abstractions.Persistence;
using Fip.Application.Abstractions.Flights;

namespace Fip.Application.Flights;

public sealed class FlightQueryService(
    IFlightRepository flightRepository,
    IFlightSummaryCalculator flightSummaryCalculator) : IFlightQueryService
{
    public async Task<IReadOnlyList<FlightListItemDto>> GetFlightsAsync(
        CancellationToken cancellationToken = default)
    {
        var flights = await flightRepository.GetAllAsync(cancellationToken);

        return flights
            .OrderByDescending(flight => flight.StartTime)
            .Select(MapListItem)
            .ToList();
    }

    public async Task<FlightDetailDto?> GetFlightByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var flight = await flightRepository.GetSummaryByIdAsync(id, cancellationToken);

        return flight is null ? null : MapDetail(flight);
    }

    public async Task<FlightSummaryDto?> GetFlightSummaryAsync(
        Guid flightId,
        CancellationToken cancellationToken = default)
    {
        var flight = await flightRepository.GetByIdAsync(flightId, cancellationToken);

        if (flight is null)
        {
            return null;
        }

        var summary = flightSummaryCalculator.Calculate(flight);

        return new FlightSummaryDto(
            flight.Id,
            flight.Callsign,
            flight.Icao24,
            flight.StartTime,
            flight.EndTime,
            summary.Duration,
            summary.MaximumAltitudeFeet,
            summary.MaximumGroundSpeedKnots,
            summary.AverageGroundSpeedKnots,
            summary.MaximumClimbRate,
            summary.MaximumDescentRate is { } descentRate ? -descentRate : null,
            summary.DistanceNauticalMiles,
            summary.TakeoffTime,
            summary.LandingTime,
            summary.FlightTime);
    }

    public async Task<IReadOnlyList<FlightTelemetryPointDto>?> GetFlightTelemetryAsync(
        Guid flightId,
        CancellationToken cancellationToken = default)
    {
        var result = await flightRepository.GetTelemetryAsync(flightId, cancellationToken);

        if (!result.FlightExists)
        {
            return null;
        }

        return result.Points
            .OrderBy(point => point.Timestamp)
            .Select(point => new FlightTelemetryPointDto(
                point.Timestamp,
                point.Latitude,
                point.Longitude,
                point.AltitudeFeet,
                point.GroundSpeedKnots,
                point.TrackDegrees,
                point.VerticalRateFeetPerMinute))
            .ToList();
    }

    public async Task<IReadOnlyList<FlightEventDto>?> GetFlightEventsAsync(
        Guid flightId,
        CancellationToken cancellationToken = default)
    {
        var result = await flightRepository.GetEventsAsync(flightId, cancellationToken);

        if (!result.FlightExists)
        {
            return null;
        }

        return result.Events
            .OrderBy(flightEvent => flightEvent.Timestamp)
            .ThenBy(flightEvent => flightEvent.Id)
            .Select(flightEvent => new FlightEventDto(
                flightEvent.Id,
                flightEvent.FlightId,
                flightEvent.Type.ToString(),
                flightEvent.Timestamp,
                flightEvent.Latitude,
                flightEvent.Longitude,
                flightEvent.AltitudeFeet,
                flightEvent.Description))
            .ToList();
    }

    private static FlightListItemDto MapListItem(FlightQueryModel flight) => new(
        flight.Id,
        flight.Icao24,
        flight.Callsign,
        flight.StartTime,
        flight.EndTime,
        flight.EndTime - flight.StartTime,
        flight.MaximumAltitudeFeet,
        flight.DepartureLatitude,
        flight.DepartureLongitude,
        flight.ArrivalLatitude,
        flight.ArrivalLongitude,
        flight.TelemetryPointCount,
        flight.EventCount);

    private static FlightDetailDto MapDetail(FlightQueryModel flight) => new(
        flight.Id,
        flight.Icao24,
        flight.Callsign,
        flight.StartTime,
        flight.EndTime,
        flight.EndTime - flight.StartTime,
        flight.DepartureLatitude,
        flight.DepartureLongitude,
        flight.ArrivalLatitude,
        flight.ArrivalLongitude,
        flight.MaximumAltitudeFeet);
}
