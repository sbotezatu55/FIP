using Fip.Application.Abstractions.Persistence;
using Fip.Domain.Flights;
using Fip.Persistence.Context;
using Fip.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fip.Persistence.Repositories;

public sealed class FlightRepository(FipDbContext dbContext) : IFlightRepository, IFlightAnalysisRepository
{
    public async Task<IReadOnlyList<FlightQueryModel>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Flights
            .AsNoTracking()
            .OrderByDescending(flight => flight.StartTime)
            .Select(flight => new FlightQueryModel(
                flight.Id,
                flight.Icao24,
                flight.Callsign,
                flight.StartTime,
                flight.EndTime,
                flight.DepartureLatitude,
                flight.DepartureLongitude,
                flight.ArrivalLatitude,
                flight.ArrivalLongitude,
                flight.MaximumAltitudeFeet,
                flight.TelemetryPoints.Count,
                flight.Events.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<FlightQueryModel?> GetSummaryByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Flights
            .AsNoTracking()
            .Where(flight => flight.Id == id)
            .Select(flight => new FlightQueryModel(
                flight.Id,
                flight.Icao24,
                flight.Callsign,
                flight.StartTime,
                flight.EndTime,
                flight.DepartureLatitude,
                flight.DepartureLongitude,
                flight.ArrivalLatitude,
                flight.ArrivalLongitude,
                flight.MaximumAltitudeFeet,
                flight.TelemetryPoints.Count,
                flight.Events.Count))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FlightTelemetryQueryResult> GetTelemetryAsync(
        Guid flightId,
        CancellationToken cancellationToken = default)
    {
        var flightExists = await dbContext.Flights
            .AsNoTracking()
            .AnyAsync(flight => flight.Id == flightId, cancellationToken);

        if (!flightExists)
        {
            return new FlightTelemetryQueryResult(false, []);
        }

        var points = await dbContext.FlightTelemetryPoints
            .AsNoTracking()
            .Where(point => point.FlightId == flightId)
            .OrderBy(point => point.Timestamp)
            .Select(point => new FlightTelemetryQueryModel(
                point.Timestamp,
                point.Latitude,
                point.Longitude,
                point.AltitudeFeet,
                point.GroundSpeedKnots,
                point.TrackDegrees,
                point.VerticalRateFeetPerMinute))
            .ToListAsync(cancellationToken);

        return new FlightTelemetryQueryResult(true, points);
    }

    public async Task<FlightEventQueryResult> GetEventsAsync(
        Guid flightId,
        CancellationToken cancellationToken = default)
    {
        var flightExists = await dbContext.Flights
            .AsNoTracking()
            .AnyAsync(flight => flight.Id == flightId, cancellationToken);

        if (!flightExists)
        {
            return new FlightEventQueryResult(false, []);
        }

        var events = await dbContext.FlightEvents
            .AsNoTracking()
            .Where(flightEvent => flightEvent.FlightId == flightId)
            .OrderBy(flightEvent => flightEvent.Timestamp)
            .ThenBy(flightEvent => flightEvent.Id)
            .Select(flightEvent => new FlightEventQueryModel(
                flightEvent.Id,
                flightEvent.FlightId,
                flightEvent.Type,
                flightEvent.Timestamp,
                flightEvent.Latitude,
                flightEvent.Longitude,
                flightEvent.AltitudeFeet,
                flightEvent.Description))
            .ToListAsync(cancellationToken);

        return new FlightEventQueryResult(true, events);
    }

    public async Task<Flight?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Flights
            .AsNoTracking()
            .AsSplitQuery()
            .Include(flight => flight.TelemetryPoints)
            .Include(flight => flight.Events)
            .FirstOrDefaultAsync(flight => flight.Id == id, cancellationToken);

        return entity is null ? null : FlightMapper.ToDomain(entity);
    }

    public async Task<Guid?> FindExistingFlightIdAsync(
        string icao24,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Flights
            .AsNoTracking()
            .Where(flight =>
                flight.Icao24 == icao24 &&
                flight.StartTime == startTime &&
                flight.EndTime == endTime)
            .Select(flight => (Guid?)flight.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(
        Flight flight,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(flight);

        await dbContext.Flights.AddAsync(FlightMapper.ToEntity(flight), cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var flight = await dbContext.Flights
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (flight is null)
        {
            return false;
        }

        dbContext.Flights.Remove(flight);
        return true;
    }

    public async Task<bool> ReplaceEventsAsync(
        Guid flightId,
        IReadOnlyCollection<Fip.Domain.FlightEvents.FlightEvent> events,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);

        var flight = await dbContext.Flights
            .Include(candidate => candidate.Events)
            .FirstOrDefaultAsync(candidate => candidate.Id == flightId, cancellationToken);

        if (flight is null)
        {
            return false;
        }

        dbContext.FlightEvents.RemoveRange(flight.Events);

        var eventEntities = events.Select(flightEvent => new FlightEventEntity
        {
            FlightId = flightId,
            Type = flightEvent.Type,
            Timestamp = flightEvent.Timestamp,
            Latitude = flightEvent.TelemetryPoint?.Latitude,
            Longitude = flightEvent.TelemetryPoint?.Longitude,
            AltitudeFeet = flightEvent.TelemetryPoint?.AltitudeFeet,
            Description = flightEvent.Description
        });

        await dbContext.FlightEvents.AddRangeAsync(eventEntities, cancellationToken);
        return true;
    }
}
