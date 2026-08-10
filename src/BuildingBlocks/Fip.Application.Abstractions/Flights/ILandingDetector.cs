using Fip.Domain.FlightEvents;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Abstractions.Flights;

public interface ILandingDetector
{
    FlightEvent? Detect(IReadOnlyList<FlightTelemetryPoint> telemetryPoints);
}
