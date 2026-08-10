namespace Fip.Application.Imports.ImportFlightTrajectory;

public interface IImportFlightTrajectoryService
{
    Task<ImportFlightTrajectoryResult> ImportAsync(
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<ImportFlightTrajectoryResult> ImportAsync(
        ImportFlightTrajectoryRequest request,
        CancellationToken cancellationToken = default);
}
