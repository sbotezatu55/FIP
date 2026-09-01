using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Fip.Persistence.Context;

public sealed class FipDbContextFactory : IDesignTimeDbContextFactory<FipDbContext>
{
    public FipDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ConnectionString-AppDb")
            ?? throw new InvalidOperationException(
                "The 'ConnectionStrings__ConnectionString-AppDb' environment variable must be configured for design-time database operations.");

        var optionsBuilder = new DbContextOptionsBuilder<FipDbContext>();
        optionsBuilder.UseSqlServer(
            connectionString,
            sql => sql.MigrationsAssembly(typeof(FipDbContext).Assembly.FullName));

        return new FipDbContext(optionsBuilder.Options);
    }
}
