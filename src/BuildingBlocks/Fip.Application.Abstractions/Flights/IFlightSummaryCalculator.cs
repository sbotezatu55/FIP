using Fip.Domain.Flights;

namespace Fip.Application.Abstractions.Flights;

public interface IFlightSummaryCalculator
{
    FlightSummary Calculate(Flight flight);
}
