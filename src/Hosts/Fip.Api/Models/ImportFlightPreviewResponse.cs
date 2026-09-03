using Fip.Application.Imports.ImportFlightPreview;

namespace Fip.Api.Models;

public sealed record ImportFlightPreviewResponse(
    Guid PreviewId,
    IReadOnlyList<ImportFlightCandidateResponse> Candidates,
    string Source,
    string Filename,
    int RecordsRead,
    int DuplicateRecordsRemoved)
{
    public static ImportFlightPreviewResponse FromResult(ImportFlightPreviewResult result) => new(
        result.PreviewId,
        result.Candidates.Select(ImportFlightCandidateResponse.FromCandidate).ToList(),
        result.Source,
        result.Filename,
        result.RecordsRead,
        result.DuplicateRecordsRemoved);
}

public sealed record ImportFlightCandidateResponse(
    Guid CandidateId,
    string? Callsign,
    string Icao24,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    int Points,
    string Status)
{
    public static ImportFlightCandidateResponse FromCandidate(ImportFlightCandidate candidate) => new(
        candidate.CandidateId, candidate.Callsign, candidate.Icao24, candidate.StartTime,
        candidate.EndTime, candidate.Points, candidate.Status.ToString());
}
