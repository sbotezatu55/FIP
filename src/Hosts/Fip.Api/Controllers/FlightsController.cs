using Fip.Application.Flights;
using Fip.Application.Abstractions.Flights;
using Fip.Application.Imports.ImportFlightTrajectory;
using Fip.Application.Imports.ImportFlightPreview;
using Fip.Api.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Fip.Api.Controllers;

[ApiController]
[Route("api/flights")]
public sealed class FlightsController(
    IFlightQueryService flightQueryService,
    IImportFlightTrajectoryService? importFlightTrajectoryService = null,
    IFlightAnalysisService? flightAnalysisService = null,
    IImportFlightPreviewService? importFlightPreviewService = null,
    IFlightDeletionService? flightDeletionService = null) : ControllerBase
{
    [HttpPost("import/preview")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ImportFlightPreviewResponse>> PreviewImport(
        [FromForm] ImportFlightRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0) return BadRequest(new { message = "A non-empty Parquet file is required." });
        if (importFlightPreviewService is null) throw new InvalidOperationException("The flight preview service is not configured.");
        var fileName = Path.GetFileName(request.File.FileName);
        if (!Path.GetExtension(fileName).Equals(".parquet", StringComparison.OrdinalIgnoreCase)) return BadRequest(new { message = "Only ADSBiq Parquet files are supported." });
        try
        {
            await using var stream = request.File.OpenReadStream();
            var result = await importFlightPreviewService.PreviewAsync(fileName, stream, cancellationToken);
            return Ok(ImportFlightPreviewResponse.FromResult(result));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("import/preview/{previewId:guid}/{candidateId:guid}")]
    public async Task<ActionResult<ImportFlightTrajectoryResponse>> ImportPreviewCandidate(Guid previewId, Guid candidateId, CancellationToken cancellationToken)
    {
        if (importFlightPreviewService is null) throw new InvalidOperationException("The flight preview service is not configured.");
        var result = await importFlightPreviewService.ImportCandidateAsync(previewId, candidateId, cancellationToken);
        return result is null ? NotFound() : Ok(ImportFlightTrajectoryResponse.FromResult(result));
    }

    [HttpDelete("import/preview/{previewId:guid}/{candidateId:guid}")]
    public ActionResult IgnorePreviewCandidate(Guid previewId, Guid candidateId)
    {
        if (importFlightPreviewService is null) throw new InvalidOperationException("The flight preview service is not configured.");
        return importFlightPreviewService.IgnoreCandidate(previewId, candidateId) ? NoContent() : NotFound();
    }
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ImportFlightTrajectoryResponse>> ImportFlight(
        [FromForm] ImportFlightRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null)
        {
            return BadRequest(new { message = "A JSON trajectory file is required." });
        }

        if (request.File.Length == 0)
        {
            return BadRequest(new { message = "The uploaded trajectory file cannot be empty." });
        }

        if (string.IsNullOrWhiteSpace(request.File.FileName))
        {
            return BadRequest(new { message = "The uploaded trajectory file must have a file name." });
        }

        var safeFileName = Path.GetFileName(request.File.FileName);
        var isJsonFile = Path.GetExtension(safeFileName).Equals(".json", StringComparison.OrdinalIgnoreCase);
        var isJsonContent = string.IsNullOrWhiteSpace(request.File.ContentType) ||
            request.File.ContentType.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
            request.File.ContentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase);

        if (!isJsonFile || !isJsonContent)
        {
            return BadRequest(new { message = "Only OpenSky JSON trajectory files are supported." });
        }

        if (importFlightTrajectoryService is null)
        {
            throw new InvalidOperationException("The flight import service is not configured.");
        }

        try
        {
            await using var stream = request.File.OpenReadStream();
            var result = await importFlightTrajectoryService.ImportAsync(
                safeFileName,
                stream,
                cancellationToken);

            if (result.Status == ImportFlightTrajectoryStatus.Duplicate)
            {
                return Ok(ImportFlightTrajectoryResponse.FromResult(result));
            }

            return CreatedAtAction(
                nameof(GetFlightById),
                new { id = result.FlightId },
                ImportFlightTrajectoryResponse.FromResult(result));
        }
        catch (JsonException exception)
        {
            return BadRequest(new { message = $"The uploaded file is not valid JSON: {exception.Message}" });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FlightListItemDto>>> GetFlights(
        CancellationToken cancellationToken)
    {
        var flights = await flightQueryService.GetFlightsAsync(cancellationToken);

        return Ok(flights);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FlightDetailDto>> GetFlightById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var flight = await flightQueryService.GetFlightByIdAsync(id, cancellationToken);

        return flight is null ? NotFound() : Ok(flight);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteFlight(Guid id, CancellationToken cancellationToken)
    {
        if (flightDeletionService is null)
        {
            throw new InvalidOperationException("The flight deletion service is not configured.");
        }

        return await flightDeletionService.DeleteAsync(id, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    [HttpGet("{id:guid}/summary")]
    public async Task<ActionResult<FlightSummaryDto>> GetFlightSummary(
        Guid id,
        CancellationToken cancellationToken)
    {
        var summary = await flightQueryService.GetFlightSummaryAsync(id, cancellationToken);

        return summary is null ? NotFound() : Ok(summary);
    }

    [HttpGet("{id:guid}/telemetry")]
    public async Task<ActionResult<IReadOnlyList<FlightTelemetryPointDto>>> GetFlightTelemetry(
        Guid id,
        CancellationToken cancellationToken)
    {
        var telemetry = await flightQueryService.GetFlightTelemetryAsync(id, cancellationToken);

        return telemetry is null ? NotFound() : Ok(telemetry);
    }

    [HttpGet("{id:guid}/events")]
    public async Task<ActionResult<IReadOnlyList<FlightEventDto>>> GetFlightEvents(
        Guid id,
        CancellationToken cancellationToken)
    {
        var events = await flightQueryService.GetFlightEventsAsync(id, cancellationToken);

        return events is null ? NotFound() : Ok(events);
    }

    [HttpPost("{id:guid}/reprocess")]
    public async Task<ActionResult<FlightAnalysisResult>> ReprocessFlight(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (flightAnalysisService is null)
        {
            throw new InvalidOperationException("The flight analysis service is not configured.");
        }

        try
        {
            var result = await flightAnalysisService.RecalculateAsync(
                id,
                SupportedFlightDataType.OpenSky,
                cancellationToken);

            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
