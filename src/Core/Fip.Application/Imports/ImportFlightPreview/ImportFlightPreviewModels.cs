using Fip.Domain.Flights;

namespace Fip.Application.Imports.ImportFlightPreview;

public enum ImportFlightCandidateStatus
{
    Complete,
    PartialStart,
    PartialEnd,
    TooShort
}

public sealed record ImportFlightCandidate(
    Guid CandidateId,
    string? Callsign,
    string Icao24,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    int Points,
    ImportFlightCandidateStatus Status,
    Flight Flight);

public sealed record ImportFlightPreviewResult(
    Guid PreviewId,
    IReadOnlyList<ImportFlightCandidate> Candidates,
    string Source,
    string Filename,
    int RecordsRead,
    int DuplicateRecordsRemoved);
