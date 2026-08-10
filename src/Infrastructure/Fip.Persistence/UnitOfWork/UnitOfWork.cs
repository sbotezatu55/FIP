using Fip.Application.Abstractions.Persistence;
using Fip.Persistence.Context;

namespace Fip.Persistence.UnitOfWork;

public sealed class UnitOfWork(FipDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
