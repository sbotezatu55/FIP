using System.Collections.Concurrent;

namespace Fip.Application.Imports.ImportFlightPreview;

public sealed class ImportFlightPreviewStore : IImportFlightPreviewStore
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, ImportFlightCandidate>> previews = [];

    public void Add(Guid previewId, IReadOnlyDictionary<Guid, ImportFlightCandidate> candidates) =>
        previews[previewId] = new ConcurrentDictionary<Guid, ImportFlightCandidate>(candidates);

    public ImportFlightCandidate? Get(Guid previewId, Guid candidateId) =>
        previews.TryGetValue(previewId, out var candidates) && candidates.TryGetValue(candidateId, out var candidate) ? candidate : null;

    public bool Remove(Guid previewId, Guid candidateId) =>
        previews.TryGetValue(previewId, out var candidates) && candidates.TryRemove(candidateId, out _);
}
