using Fip.Application.Abstractions.Persistence;

namespace Fip.Application.Flights;

public sealed class FlightDeletionService(
    IFlightRepository flightRepository,
    IUnitOfWork unitOfWork) : IFlightDeletionService
{
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await flightRepository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return false;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
