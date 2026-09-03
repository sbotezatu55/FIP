using Fip.Application.Abstractions.Flights;
using Fip.Application.Abstractions.Persistence;

namespace Fip.Application.Flights;

/// <summary>
/// Recalculates derived flight analysis from persisted normalized telemetry.
/// </summary>
public sealed class FlightAnalysisService(
    IFlightRepository flightRepository,
    IFlightAnalysisRepository flightAnalysisRepository,
    IUnitOfWork unitOfWork,
    IFlightEventDetectionService eventDetectionService) : IFlightAnalysisService
{
    public async Task<FlightAnalysisResult?> RecalculateAsync(
        Guid flightId,
        SupportedFlightDataType dataType = SupportedFlightDataType.OpenSky,
        CancellationToken cancellationToken = default)
    {
        if (dataType != SupportedFlightDataType.OpenSky)
        {
            throw new ArgumentOutOfRangeException(nameof(dataType), dataType, "The selected flight-data type is not supported.");
        }

        var flight = await flightRepository.GetByIdAsync(flightId, cancellationToken);
        if (flight is null)
        {
            return null;
        }

        var events = eventDetectionService.Detect(flight.TelemetryPoints);
        await flightAnalysisRepository.ReplaceEventsAsync(flightId, events, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new FlightAnalysisResult(flightId, dataType, events.Count);
    }
}
