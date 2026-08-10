using Fip.Api.Controllers;
using Fip.Api.Models;
using Fip.Application.Imports.ImportFlightTrajectory;
using Fip.Application.Flights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;

namespace Fip.IntegrationTests;

public sealed class FlightsControllerTests
{
    [Fact]
    public async Task GetFlights_ReturnsOkWithEmptyArray()
    {
        var controller = new FlightsController(new FakeFlightQueryService());

        var actionResult = await controller.GetFlights(CancellationToken.None);

        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var flights = Assert.IsAssignableFrom<IReadOnlyList<FlightListItemDto>>(result.Value);
        Assert.Empty(flights);
    }

    [Fact]
    public async Task GetFlights_ReturnsStoredFlights()
    {
        var flight = CreateListItem();
        var controller = new FlightsController(new FakeFlightQueryService { Flights = [flight] });

        var actionResult = await controller.GetFlights(CancellationToken.None);

        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var flights = Assert.IsAssignableFrom<IReadOnlyList<FlightListItemDto>>(result.Value);
        Assert.Equal(flight, Assert.Single(flights));
    }

    [Fact]
    public async Task GetFlightById_WhenExisting_ReturnsOk()
    {
        var flight = CreateDetail();
        var controller = new FlightsController(new FakeFlightQueryService { Flight = flight });

        var actionResult = await controller.GetFlightById(flight.Id, CancellationToken.None);

        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(flight, result.Value);
    }

    [Fact]
    public async Task GetFlightById_WhenMissing_ReturnsNotFound()
    {
        var controller = new FlightsController(new FakeFlightQueryService());

        var actionResult = await controller.GetFlightById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetFlightSummary_WhenExisting_ReturnsOk()
    {
        var summary = new FlightSummaryDto(
            Guid.NewGuid(),
            "TRA051",
            "484506",
            DateTimeOffset.Parse("2026-08-09T12:00:00Z"),
            DateTimeOffset.Parse("2026-08-09T13:00:00Z"),
            TimeSpan.FromHours(1),
            38_000,
            472,
            318,
            1_200,
            -1_800,
            512,
            null,
            null,
            null);
        var controller = new FlightsController(new FakeFlightQueryService { Summary = summary });

        var actionResult = await controller.GetFlightSummary(summary.FlightId, CancellationToken.None);

        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(summary, result.Value);
    }

    [Fact]
    public async Task GetFlightSummary_WhenMissing_ReturnsNotFound()
    {
        var controller = new FlightsController(new FakeFlightQueryService());

        var actionResult = await controller.GetFlightSummary(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetFlightTelemetry_WhenExisting_ReturnsOkWithoutAggregateGraph()
    {
        var telemetry = new[]
        {
            new FlightTelemetryPointDto(
                DateTimeOffset.Parse("2026-08-09T12:00:00Z"),
                52.1,
                4.7,
                224,
                155,
                3,
                2_240)
        };
        var controller = new FlightsController(new FakeFlightQueryService { Telemetry = telemetry });

        var actionResult = await controller.GetFlightTelemetry(Guid.NewGuid(), CancellationToken.None);

        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var points = Assert.IsAssignableFrom<IReadOnlyList<FlightTelemetryPointDto>>(result.Value);
        Assert.Equal(telemetry, points);
    }

    [Fact]
    public async Task GetFlightTelemetry_WhenKnownFlightHasNoPoints_ReturnsEmptyOkArray()
    {
        var controller = new FlightsController(new FakeFlightQueryService { Telemetry = [] });

        var actionResult = await controller.GetFlightTelemetry(Guid.NewGuid(), CancellationToken.None);

        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var points = Assert.IsAssignableFrom<IReadOnlyList<FlightTelemetryPointDto>>(result.Value);
        Assert.Empty(points);
    }

    [Fact]
    public async Task GetFlightTelemetry_WhenMissing_ReturnsNotFound()
    {
        var controller = new FlightsController(new FakeFlightQueryService());

        var actionResult = await controller.GetFlightTelemetry(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(actionResult.Result);
    }

    [Fact]
    public async Task GetFlightEvents_WhenExisting_ReturnsChronologicalDtoArray()
    {
        var events = new[]
        {
            new FlightEventDto(Guid.NewGuid(), Guid.NewGuid(), "Takeoff", DateTimeOffset.Parse("2026-08-09T12:00:00Z"), 52.1, 4.7, 450, "Detected takeoff"),
            new FlightEventDto(Guid.NewGuid(), Guid.NewGuid(), "Landing", DateTimeOffset.Parse("2026-08-09T13:00:00Z"), null, null, null, null)
        };
        var controller = new FlightsController(new FakeFlightQueryService { Events = events });

        var actionResult = await controller.GetFlightEvents(Guid.NewGuid(), CancellationToken.None);

        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returnedEvents = Assert.IsAssignableFrom<IReadOnlyList<FlightEventDto>>(result.Value);
        Assert.Equal(events, returnedEvents);
        Assert.Equal("Takeoff", returnedEvents[0].Type);
    }

    [Fact]
    public async Task GetFlightEvents_WhenKnownFlightHasNoEvents_ReturnsEmptyOkArray()
    {
        var controller = new FlightsController(new FakeFlightQueryService { Events = [] });

        var actionResult = await controller.GetFlightEvents(Guid.NewGuid(), CancellationToken.None);

        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var events = Assert.IsAssignableFrom<IReadOnlyList<FlightEventDto>>(result.Value);
        Assert.Empty(events);
    }

    [Fact]
    public async Task GetFlightEvents_WhenMissing_ReturnsNotFound()
    {
        var controller = new FlightsController(new FakeFlightQueryService());

        var actionResult = await controller.GetFlightEvents(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(actionResult.Result);
    }

    [Fact]
    public async Task ImportFlight_WhenValidFile_ReturnsCreatedResult()
    {
        var flightId = Guid.NewGuid();
        var importResult = new ImportFlightTrajectoryResult(
            ImportFlightTrajectoryStatus.Imported,
            flightId,
            "TRA051",
            "484506",
            2,
            DateTimeOffset.Parse("2026-08-09T12:00:00Z"),
            DateTimeOffset.Parse("2026-08-09T13:00:00Z"),
            1,
            []);
        var importer = new FakeImportFlightTrajectoryService { Result = importResult };
        var controller = new FlightsController(new FakeFlightQueryService(), importer);
        var request = new ImportFlightRequest
        {
            File = CreateFormFile("trajectory.json", "[]")
        };

        var actionResult = await controller.ImportFlight(request, CancellationToken.None);

        var result = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        Assert.Equal(nameof(FlightsController.GetFlightById), result.ActionName);
        Assert.Equal(importResult, result.Value);
        Assert.Equal("trajectory.json", importer.FileName);
    }

    [Fact]
    public async Task ImportFlight_WhenEmptyFile_ReturnsBadRequest()
    {
        var controller = new FlightsController(
            new FakeFlightQueryService(),
            new FakeImportFlightTrajectoryService());
        var request = new ImportFlightRequest
        {
            File = CreateFormFile("trajectory.json", string.Empty)
        };

        var actionResult = await controller.ImportFlight(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task ImportFlight_WhenMalformedJson_ReturnsBadRequest()
    {
        var controller = new FlightsController(
            new FakeFlightQueryService(),
            new FakeImportFlightTrajectoryService { Exception = new JsonException("malformed") });
        var request = new ImportFlightRequest
        {
            File = CreateFormFile("trajectory.json", "{invalid")
        };

        var actionResult = await controller.ImportFlight(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    [Fact]
    public async Task ImportFlight_WhenDuplicate_ReturnsOkWithExistingResult()
    {
        var duplicate = new ImportFlightTrajectoryResult(
            ImportFlightTrajectoryStatus.Duplicate,
            Guid.NewGuid(),
            "TRA051",
            "484506",
            0,
            DateTimeOffset.Parse("2026-08-09T12:00:00Z"),
            DateTimeOffset.Parse("2026-08-09T13:00:00Z"),
            0,
            ["Flight already exists."]);
        var controller = new FlightsController(
            new FakeFlightQueryService(),
            new FakeImportFlightTrajectoryService { Result = duplicate });
        var request = new ImportFlightRequest
        {
            File = CreateFormFile("trajectory.json", "[]")
        };

        var actionResult = await controller.ImportFlight(request, CancellationToken.None);

        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(duplicate, result.Value);
    }

    private static FormFile CreateFormFile(string fileName, string contents)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(contents));
        return new FormFile(stream, 0, stream.Length, "File", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/json"
        };
    }

    private static FlightListItemDto CreateListItem() => new(
        Guid.NewGuid(),
        "484506",
        "TRA051",
        DateTimeOffset.Parse("2026-08-09T12:00:00Z"),
        DateTimeOffset.Parse("2026-08-09T13:00:00Z"),
        TimeSpan.FromHours(1),
        35_000,
        52.1,
        4.2,
        51.5,
        0.1,
        12,
        3);

    private static FlightDetailDto CreateDetail() => new(
        Guid.NewGuid(),
        "484506",
        "TRA051",
        DateTimeOffset.Parse("2026-08-09T12:00:00Z"),
        DateTimeOffset.Parse("2026-08-09T13:00:00Z"),
        TimeSpan.FromHours(1),
        52.1,
        4.2,
        51.5,
        0.1,
        35_000);

    private sealed class FakeFlightQueryService : IFlightQueryService
    {
        public IReadOnlyList<FlightListItemDto> Flights { get; init; } = [];
        public FlightDetailDto? Flight { get; init; }
        public FlightSummaryDto? Summary { get; init; }
        public IReadOnlyList<FlightTelemetryPointDto>? Telemetry { get; init; }
        public IReadOnlyList<FlightEventDto>? Events { get; init; }

        public Task<IReadOnlyList<FlightListItemDto>> GetFlightsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Flights);

        public Task<FlightDetailDto?> GetFlightByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Flight?.Id == id ? Flight : null);

        public Task<FlightSummaryDto?> GetFlightSummaryAsync(Guid flightId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Summary?.FlightId == flightId ? Summary : null);

        public Task<IReadOnlyList<FlightTelemetryPointDto>?> GetFlightTelemetryAsync(Guid flightId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Telemetry);

        public Task<IReadOnlyList<FlightEventDto>?> GetFlightEventsAsync(Guid flightId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Events);
    }

    private sealed class FakeImportFlightTrajectoryService : IImportFlightTrajectoryService
    {
        public ImportFlightTrajectoryResult Result { get; init; } = new(
            ImportFlightTrajectoryStatus.Imported,
            Guid.NewGuid(),
            "TRA051",
            "484506",
            1,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(1),
            0,
            []);
        public Exception? Exception { get; init; }
        public string? FileName { get; private set; }

        public Task<ImportFlightTrajectoryResult> ImportAsync(
            string fileName,
            Stream content,
            CancellationToken cancellationToken = default)
        {
            FileName = fileName;
            if (Exception is not null)
            {
                throw Exception;
            }

            return Task.FromResult(Result);
        }

        public Task<ImportFlightTrajectoryResult> ImportAsync(
            ImportFlightTrajectoryRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result);
    }
}
