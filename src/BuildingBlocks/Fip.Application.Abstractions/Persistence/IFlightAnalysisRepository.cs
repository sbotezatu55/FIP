using Fip.Domain.FlightEvents;

namespace Fip.Application.Abstractions.Persistence;

/// <summary>
/// Persistence operations used to replace derived analysis for an existing flight.
/// </summary>
public interface IFlightAnalysisRepository
{
    Task<bool> ReplaceEventsAsync(
        Guid flightId,
        IReadOnlyCollection<FlightEvent> events,
        CancellationToken cancellationToken = default);
}
