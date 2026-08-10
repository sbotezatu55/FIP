using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Abstractions.Telemetry;

public interface ITelemetryGapDetector
{
    IReadOnlyList<TelemetryGap> Detect(IReadOnlyList<FlightTelemetryPoint> telemetryPoints);
}
