using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Fip.Persistence.Context;

public sealed class FipDbContextFactory : IDesignTimeDbContextFactory<FipDbContext>
{
    private const string DefaultDevelopmentConnectionString =
        "Server=DESKTOP-BIRS3VI\\SQLEXPRESS;Database=FIP;Trusted_Connection=True;TrustServerCertificate=True;";

    public FipDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ConnectionString-AppDb")
            ?? DefaultDevelopmentConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<FipDbContext>();
        optionsBuilder.UseSqlServer(
            connectionString,
            sql => sql.MigrationsAssembly(typeof(FipDbContext).Assembly.FullName));

        return new FipDbContext(optionsBuilder.Options);
    }
}
