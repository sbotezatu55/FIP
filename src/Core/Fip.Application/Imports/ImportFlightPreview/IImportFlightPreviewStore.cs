namespace Fip.Application.Imports.ImportFlightPreview;

public interface IImportFlightPreviewStore
{
    void Add(Guid previewId, IReadOnlyDictionary<Guid, ImportFlightCandidate> candidates);
    ImportFlightCandidate? Get(Guid previewId, Guid candidateId);
    bool Remove(Guid previewId, Guid candidateId);
}
