using Fip.Domain.FlightEvents;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Abstractions.Flights;

public interface ITakeoffDetector
{
    FlightEvent? Detect(IReadOnlyList<FlightTelemetryPoint> telemetryPoints);
}
