using Microsoft.Extensions.DependencyInjection;
using Fip.Application.Flights.Import.OpenSky;
using Fip.Infrastructure.Flights.Import.OpenSky;
using Fip.Application.Flights.Import.AdsbIq;
using Fip.Infrastructure.Flights.Import.AdsbIq;

namespace Fip.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IOpenSkyTrajectoryImporter, OpenSkyJsonTrajectoryImporter>();
        services.AddSingleton<IAdsbIqTelemetryImporter, AdsbIqParquetTelemetryImporter>();

        return services;
    }
}
