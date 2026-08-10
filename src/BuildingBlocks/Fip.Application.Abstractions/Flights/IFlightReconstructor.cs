using Fip.Domain.Flights;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Abstractions.Flights;

public interface IFlightReconstructor
{
    Flight Reconstruct(IReadOnlyList<FlightTelemetryPoint> telemetryPoints);
}
