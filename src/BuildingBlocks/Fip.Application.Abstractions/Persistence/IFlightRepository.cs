using Fip.Domain.Flights;

namespace Fip.Application.Abstractions.Persistence;

/// <summary>
/// Application-facing persistence operations for reconstructed flights.
/// </summary>
public interface IFlightRepository
{
    /// <summary>
    /// Retrieves flight summary projections in newest-first order.
    /// </summary>
    Task<IReadOnlyList<FlightQueryModel>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves one flight summary projection without loading child collections.
    /// </summary>
    Task<FlightQueryModel?> GetSummaryByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves normalized telemetry for one flight without loading its aggregate graph.
    /// </summary>
    Task<FlightTelemetryQueryResult> GetTelemetryAsync(
        Guid flightId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves stored events for one flight without loading its aggregate graph.
    /// </summary>
    Task<FlightEventQueryResult> GetEventsAsync(
        Guid flightId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a reconstructed flight aggregate, including its telemetry and events,
    /// by its domain identifier.
    /// </summary>
    Task<Flight?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a previously persisted flight with the same reconstructed identity.
    /// </summary>
    Task<Guid?> FindExistingFlightIdAsync(
        string icao24,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a newly reconstructed flight for persistence.
    /// </summary>
    Task AddAsync(
        Flight flight,
        CancellationToken cancellationToken = default);
}
