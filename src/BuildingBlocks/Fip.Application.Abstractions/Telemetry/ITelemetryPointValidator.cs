using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Abstractions.Telemetry;

public interface ITelemetryPointValidator
{
    TelemetryValidationResult Validate(FlightTelemetryPoint telemetryPoint);
}
