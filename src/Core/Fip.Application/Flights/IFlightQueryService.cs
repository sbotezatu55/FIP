namespace Fip.Application.Flights;

public interface IFlightQueryService
{
    Task<IReadOnlyList<FlightListItemDto>> GetFlightsAsync(
        CancellationToken cancellationToken = default);

    Task<FlightDetailDto?> GetFlightByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<FlightSummaryDto?> GetFlightSummaryAsync(
        Guid flightId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FlightTelemetryPointDto>?> GetFlightTelemetryAsync(
        Guid flightId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FlightEventDto>?> GetFlightEventsAsync(
        Guid flightId,
        CancellationToken cancellationToken = default);
}
