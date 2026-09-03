namespace Fip.Application.Abstractions.Flights;

/// <summary>
/// Supported normalized flight-data sources for recalculation.
/// Extend this enum when another source is supported by the platform.
/// </summary>
public enum SupportedFlightDataType
{
    OpenSky = 0
}

public sealed record FlightAnalysisResult(
    Guid FlightId,
    SupportedFlightDataType DataType,
    int EventsDetected);

public interface IFlightAnalysisService
{
    Task<FlightAnalysisResult?> RecalculateAsync(
        Guid flightId,
        SupportedFlightDataType dataType = SupportedFlightDataType.OpenSky,
        CancellationToken cancellationToken = default);
}
