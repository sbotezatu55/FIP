using Fip.Domain.FlightEvents;
using Fip.Domain.Flights.Phases;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Abstractions.Flights;

public interface IFlightPhaseClassifier
{
    IReadOnlyCollection<FlightPhaseSegment> Classify(
        IReadOnlyList<FlightTelemetryPoint> telemetryPoints,
        IReadOnlyCollection<FlightEvent> events);
}
