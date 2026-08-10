using Fip.Domain.FlightEvents;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Abstractions.Flights;

public interface IFlightEventDetector
{
    IReadOnlyCollection<FlightEvent> Detect(IReadOnlyList<FlightTelemetryPoint> telemetryPoints);
}
