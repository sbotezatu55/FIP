using Microsoft.Extensions.DependencyInjection;
using Fip.Application.Flights.Import.OpenSky;
using Fip.Infrastructure.Flights.Import.OpenSky;

namespace Fip.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IOpenSkyTrajectoryImporter, OpenSkyJsonTrajectoryImporter>();

        return services;
    }
}
