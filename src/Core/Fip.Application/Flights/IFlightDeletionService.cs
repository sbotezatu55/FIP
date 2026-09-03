namespace Fip.Application.Flights;

public interface IFlightDeletionService
{
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
