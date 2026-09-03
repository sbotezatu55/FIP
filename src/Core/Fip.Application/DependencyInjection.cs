using Microsoft.Extensions.DependencyInjection;
using Fip.Application.Abstractions.Flights;
using Fip.Application.Abstractions.Telemetry;
using Fip.Application.Aircraft;
using Fip.Application.FlightEvents;
using Fip.Application.Flights;
using Fip.Application.Imports;
using Fip.Application.Imports.ImportFlightTrajectory;
using Fip.Application.Imports.ImportFlightPreview;
using Fip.Application.Telemetry;
using Fip.SharedKernel.Geography;

namespace Fip.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<AircraftService>();
        services.AddSingleton<ImportFlightService>();
        services.AddScoped<IImportFlightTrajectoryService, ImportFlightTrajectoryService>();
        services.AddScoped<IImportFlightPreviewService, ImportFlightPreviewService>();
        services.AddSingleton<IImportFlightPreviewStore, ImportFlightPreviewStore>();
        services.AddScoped<IFlightQueryService, FlightQueryService>();
        services.AddScoped<IFlightDeletionService, FlightDeletionService>();
        services.AddScoped<IFlightAnalysisService, FlightAnalysisService>();
        services.AddSingleton<FlightService>();
        services.AddSingleton<IFlightReconstructor, FlightReconstructor>();
        services.AddSingleton<IFlightSummaryCalculator, FlightSummaryCalculator>();
        services.AddSingleton<IGeoDistanceCalculator, GeoDistanceCalculator>();
        services.AddSingleton<TakeoffDetector>();
        services.AddSingleton<ITakeoffDetector>(services => services.GetRequiredService<TakeoffDetector>());
        services.AddSingleton<IFlightEventDetector>(services => services.GetRequiredService<TakeoffDetector>());
        services.AddSingleton<LandingDetector>();
        services.AddSingleton<ILandingDetector>(services => services.GetRequiredService<LandingDetector>());
        services.AddSingleton<IFlightEventDetector>(services => services.GetRequiredService<LandingDetector>());
        services.AddSingleton<TopOfDescentDetector>();
        services.AddSingleton<ITopOfDescentDetector>(services => services.GetRequiredService<TopOfDescentDetector>());
        services.AddSingleton<IFlightEventDetector>(services => services.GetRequiredService<TopOfDescentDetector>());
        services.AddSingleton<TopOfClimbDetector>();
        services.AddSingleton<ITopOfClimbDetector>(services => services.GetRequiredService<TopOfClimbDetector>());
        services.AddSingleton<IFlightEventDetector>(services => services.GetRequiredService<TopOfClimbDetector>());
        services.AddSingleton<IFlightEventDetectionService, FlightEventDetectionService>();
        services.AddSingleton<IFlightPhaseClassifier, FlightPhaseClassifier>();
        services.AddSingleton<ITelemetryPointValidator, TelemetryPointValidator>();
        services.AddSingleton<ITelemetryGapDetector, TelemetryGapDetector>();
        services.AddSingleton<TelemetryService>();
        services.AddSingleton<FlightEventService>();

        return services;
    }
}
