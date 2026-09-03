namespace Fip.Application.Imports.ImportFlightPreview;

using Fip.Application.Imports.ImportFlightTrajectory;

public interface IImportFlightPreviewService
{
    Task<ImportFlightPreviewResult> PreviewAsync(string fileName, Stream content, CancellationToken cancellationToken = default);
    Task<ImportFlightCandidate?> GetCandidateAsync(Guid previewId, Guid candidateId, CancellationToken cancellationToken = default);
    Task<ImportFlightTrajectoryResult?> ImportCandidateAsync(Guid previewId, Guid candidateId, CancellationToken cancellationToken = default);
    bool IgnoreCandidate(Guid previewId, Guid candidateId);
}
