using Fip.Application.Abstractions.Persistence;
using Fip.Persistence.Context;
using Fip.Persistence.Repositories;
using Fip.Persistence.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fip.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("ConnectionString-AppDb")
            ?? throw new InvalidOperationException(
                "The 'ConnectionString-AppDb' connection string has not been configured.");

        services.AddDbContext<FipDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly(typeof(FipDbContext).Assembly.FullName)));

        services.AddScoped<FlightRepository>();
        services.AddScoped<IFlightRepository>(services => services.GetRequiredService<FlightRepository>());
        services.AddScoped<IFlightAnalysisRepository>(services => services.GetRequiredService<FlightRepository>());
        services.AddScoped<IUnitOfWork, Fip.Persistence.UnitOfWork.UnitOfWork>();

        return services;
    }
}
