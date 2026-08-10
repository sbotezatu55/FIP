using Fip.Application.Abstractions.Flights;
using Fip.Domain.FlightEvents;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Flights;

/// <summary>
/// Runs all registered flight-event detectors and combines their results chronologically.
/// </summary>
public sealed class FlightEventDetectionService : IFlightEventDetectionService
{
    private readonly IReadOnlyCollection<IFlightEventDetector> _detectors;

    public FlightEventDetectionService(IEnumerable<IFlightEventDetector> detectors)
    {
        ArgumentNullException.ThrowIfNull(detectors);

        _detectors = detectors.ToList().AsReadOnly();
    }

    public IReadOnlyCollection<FlightEvent> Detect(IReadOnlyList<FlightTelemetryPoint> telemetryPoints)
    {
        ArgumentNullException.ThrowIfNull(telemetryPoints);

        if (telemetryPoints.Count == 0 || _detectors.Count == 0)
        {
            return Array.Empty<FlightEvent>();
        }

        var events = _detectors
            .SelectMany(detector => detector.Detect(telemetryPoints))
            .OrderBy(flightEvent => flightEvent.Timestamp)
            .ToList();

        return events.AsReadOnly();
    }
}
