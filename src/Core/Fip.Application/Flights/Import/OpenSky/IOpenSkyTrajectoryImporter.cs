namespace Fip.Application.Flights.Import.OpenSky;

public interface IOpenSkyTrajectoryImporter
{
    Task<IReadOnlyList<OpenSkyTelemetryPointDto>> ImportAsync(
        Stream content,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OpenSkyTelemetryPointDto>> ImportAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}
