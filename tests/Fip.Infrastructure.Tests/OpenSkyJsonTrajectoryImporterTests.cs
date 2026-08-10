using System.Text.Json;
using Fip.Application.Flights.Import.OpenSky;
using Fip.Infrastructure.Flights.Import.OpenSky;

namespace Fip.Infrastructure.Tests;

public sealed class OpenSkyJsonTrajectoryImporterTests
{
    [Fact]
    public async Task ImportAsync_DeserializesTelemetryPoints()
    {
        const string json = """
            [
              {
                "timestamp": 1527693698000,
                "icao24": "484506",
                "latitude": 52.3239704714,
                "longitude": 4.7394234794,
                "groundspeed": 155.0,
                "track": 3.0,
                "vertical_rate": 2240.0,
                "callsign": "TRA051",
                "altitude": 224.0
              }
            ]
            """;

        var importer = new OpenSkyJsonTrajectoryImporter();
        var filePath = await CreateTemporaryFileAsync(json);

        try
        {
            var points = await importer.ImportAsync(filePath);

            var point = Assert.Single(points);
            Assert.Equal(1527693698000, point.Timestamp);
            Assert.Equal("484506", point.Icao24);
            Assert.Equal(52.3239704714, point.Latitude);
            Assert.Equal(4.7394234794, point.Longitude);
            Assert.Equal(155.0, point.GroundSpeed);
            Assert.Equal(3.0, point.Track);
            Assert.Equal(2240.0, point.VerticalRate);
            Assert.Equal("TRA051", point.Callsign);
            Assert.Equal(224.0, point.Altitude);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ImportAsync_StreamDeserializesTelemetryPoints()
    {
        const string json = "[{\"timestamp\":1,\"icao24\":\"abc123\",\"latitude\":40.0}]";
        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        var points = await new OpenSkyJsonTrajectoryImporter().ImportAsync(stream);

        var point = Assert.Single(points);
        Assert.Equal("abc123", point.Icao24);
        Assert.Equal(40.0, point.Latitude);
    }

    [Fact]
    public async Task ImportAsync_MissingNullableValuesBecomeNull()
    {
        const string json = "[{\"timestamp\":1,\"icao24\":\"abc123\"}]";
        var importer = new OpenSkyJsonTrajectoryImporter();
        var filePath = await CreateTemporaryFileAsync(json);

        try
        {
            var point = Assert.Single(await importer.ImportAsync(filePath));

            Assert.Null(point.Latitude);
            Assert.Null(point.Longitude);
            Assert.Null(point.GroundSpeed);
            Assert.Null(point.Track);
            Assert.Null(point.VerticalRate);
            Assert.Null(point.Callsign);
            Assert.Null(point.Altitude);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ImportAsync_EmptyArrayReturnsEmptyCollection()
    {
        var importer = new OpenSkyJsonTrajectoryImporter();
        var filePath = await CreateTemporaryFileAsync("[]");

        try
        {
            var points = await importer.ImportAsync(filePath);

            Assert.Empty(points);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ImportAsync_MissingFileThrowsFileNotFoundException()
    {
        var importer = new OpenSkyJsonTrajectoryImporter();
        var filePath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.json");

        await Assert.ThrowsAsync<FileNotFoundException>(() => importer.ImportAsync(filePath));
    }

    [Fact]
    public async Task ImportAsync_InvalidJsonThrowsJsonException()
    {
        var importer = new OpenSkyJsonTrajectoryImporter();
        var filePath = await CreateTemporaryFileAsync("{invalid-json");

        try
        {
            await Assert.ThrowsAsync<JsonException>(() => importer.ImportAsync(filePath));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static async Task<string> CreateTemporaryFileAsync(string contents)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"opensky-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(filePath, contents);
        return filePath;
    }
}
