using Fip.Application.Flights.Import.OpenSky;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Tests;

public sealed class OpenSkyTelemetryMapperTests
{
    [Fact]
    public void Map_ConvertsUnixMillisecondsToDateTimeOffset()
    {
        var source = CreateSource(timestamp: 1527693698000);

        var result = OpenSkyTelemetryMapper.Map(source);

        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1527693698000), result.Timestamp);
    }

    [Fact]
    public void Map_PreservesIdentityAndPositionValues()
    {
        var source = CreateSource(
            icao24: "484506",
            callsign: "  TRA051  ",
            latitude: 52.3239704714,
            longitude: 4.7394234794);

        var result = OpenSkyTelemetryMapper.Map(source);

        Assert.Equal("484506", result.Icao24);
        Assert.Equal("TRA051", result.Callsign);
        Assert.Equal(52.3239704714, result.Latitude);
        Assert.Equal(4.7394234794, result.Longitude);
    }

    [Fact]
    public void Map_UsesExplicitNormalizedUnitNamesWithoutConvertingValues()
    {
        var source = CreateSource(
            altitude: 224.0,
            groundSpeed: 155.0,
            track: 3.0,
            verticalRate: 2240.0);

        var result = OpenSkyTelemetryMapper.Map(source);

        Assert.Equal(224.0, result.AltitudeFeet);
        Assert.Equal(155.0, result.GroundSpeedKnots);
        Assert.Equal(3.0, result.TrackDegrees);
        Assert.Equal(2240.0, result.VerticalRateFeetPerMinute);
    }

    [Fact]
    public void Map_PreservesNullTelemetryValues()
    {
        var result = OpenSkyTelemetryMapper.Map(CreateSource());

        Assert.Null(result.Callsign);
        Assert.Null(result.Latitude);
        Assert.Null(result.Longitude);
        Assert.Null(result.AltitudeFeet);
        Assert.Null(result.GroundSpeedKnots);
        Assert.Null(result.TrackDegrees);
        Assert.Null(result.VerticalRateFeetPerMinute);
    }

    [Fact]
    public void Map_CollectionPreservesCountAndOrder()
    {
        var first = CreateSource(timestamp: 1, icao24: "first");
        var second = CreateSource(timestamp: 2, icao24: "second");

        var result = OpenSkyTelemetryMapper.Map(new[] { first, second });

        Assert.Equal(2, result.Count);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1), result[0].Timestamp);
        Assert.Equal("first", result[0].Icao24);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(2), result[1].Timestamp);
        Assert.Equal("second", result[1].Icao24);
    }

    private static OpenSkyTelemetryPointDto CreateSource(
        long timestamp = 1527693698000,
        string icao24 = "484506",
        string? callsign = null,
        double? latitude = null,
        double? longitude = null,
        double? altitude = null,
        double? groundSpeed = null,
        double? track = null,
        double? verticalRate = null)
    {
        return new OpenSkyTelemetryPointDto
        {
            Timestamp = timestamp,
            Icao24 = icao24,
            Callsign = callsign,
            Latitude = latitude,
            Longitude = longitude,
            Altitude = altitude,
            GroundSpeed = groundSpeed,
            Track = track,
            VerticalRate = verticalRate
        };
    }
}
