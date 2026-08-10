using Fip.Application.Telemetry;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Tests;

public sealed class TelemetryPointValidatorTests
{
    private readonly TelemetryPointValidator _validator = new();

    [Theory]
    [InlineData(-90)]
    [InlineData(90)]
    public void Validate_AcceptsLatitudeBoundaries(double latitude)
    {
        var result = _validator.Validate(CreatePoint(latitude: latitude));

        Assert.Equal(TelemetryValidationStatus.Valid, result.Status);
        Assert.DoesNotContain(TelemetryValidationIssue.LatitudeOutOfRange, result.Issues);
    }

    [Theory]
    [InlineData(-90.01)]
    [InlineData(90.01)]
    public void Validate_RejectsLatitudeOutsideRange(double latitude)
    {
        var result = _validator.Validate(CreatePoint(latitude: latitude));

        Assert.Equal(TelemetryValidationStatus.Invalid, result.Status);
        Assert.Contains(TelemetryValidationIssue.LatitudeOutOfRange, result.Issues);
    }

    [Theory]
    [InlineData(-180)]
    [InlineData(180)]
    public void Validate_AcceptsLongitudeBoundaries(double longitude)
    {
        var result = _validator.Validate(CreatePoint(longitude: longitude));

        Assert.Equal(TelemetryValidationStatus.Valid, result.Status);
        Assert.DoesNotContain(TelemetryValidationIssue.LongitudeOutOfRange, result.Issues);
    }

    [Theory]
    [InlineData(-180.01)]
    [InlineData(180.01)]
    public void Validate_RejectsLongitudeOutsideRange(double longitude)
    {
        var result = _validator.Validate(CreatePoint(longitude: longitude));

        Assert.Equal(TelemetryValidationStatus.Invalid, result.Status);
        Assert.Contains(TelemetryValidationIssue.LongitudeOutOfRange, result.Issues);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(359.999)]
    public void Validate_AcceptsTrackBoundaries(double track)
    {
        var result = _validator.Validate(CreatePoint(trackDegrees: track));

        Assert.Equal(TelemetryValidationStatus.Valid, result.Status);
        Assert.DoesNotContain(TelemetryValidationIssue.TrackOutOfRange, result.Issues);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(360)]
    public void Validate_RejectsTrackOutsideRange(double track)
    {
        var result = _validator.Validate(CreatePoint(trackDegrees: track));

        Assert.Equal(TelemetryValidationStatus.Invalid, result.Status);
        Assert.Contains(TelemetryValidationIssue.TrackOutOfRange, result.Issues);
    }

    [Fact]
    public void Validate_RejectsDefaultTimestamp()
    {
        var result = _validator.Validate(CreatePoint(timestamp: DateTimeOffset.MinValue));

        Assert.Equal(TelemetryValidationStatus.Invalid, result.Status);
        Assert.Contains(TelemetryValidationIssue.InvalidTimestamp, result.Issues);
    }

    [Fact]
    public void Validate_AcceptsNormalTelemetry()
    {
        var result = _validator.Validate(CreatePoint(
            latitude: 28.5,
            longitude: -81.3,
            altitudeFeet: 8_000,
            trackDegrees: 120,
            groundSpeedKnots: 220,
            verticalRateFeetPerMinute: 500));

        Assert.Equal(TelemetryValidationStatus.Valid, result.Status);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Validate_ClassifiesBroadExtremeValuesAsSuspicious()
    {
        var result = _validator.Validate(CreatePoint(
            altitudeFeet: 70_001,
            groundSpeedKnots: 1_201,
            verticalRateFeetPerMinute: -15_001));

        Assert.Equal(TelemetryValidationStatus.Suspicious, result.Status);
        Assert.Contains(TelemetryValidationIssue.AltitudeUnusuallyHigh, result.Issues);
        Assert.Contains(TelemetryValidationIssue.GroundSpeedUnusuallyHigh, result.Issues);
        Assert.Contains(TelemetryValidationIssue.VerticalRateUnusuallyHigh, result.Issues);
    }

    [Fact]
    public void Validate_InvalidIssuesTakePrecedenceOverSuspiciousIssues()
    {
        var result = _validator.Validate(CreatePoint(latitude: 100, altitudeFeet: 75_000));

        Assert.Equal(TelemetryValidationStatus.Invalid, result.Status);
        Assert.Contains(TelemetryValidationIssue.LatitudeOutOfRange, result.Issues);
        Assert.Contains(TelemetryValidationIssue.AltitudeUnusuallyHigh, result.Issues);
    }

    [Fact]
    public void Validate_ReportsAllObviousIssues()
    {
        var result = _validator.Validate(CreatePoint(latitude: 100, longitude: 200, trackDegrees: 400));

        Assert.Equal(TelemetryValidationStatus.Invalid, result.Status);
        Assert.Contains(TelemetryValidationIssue.LatitudeOutOfRange, result.Issues);
        Assert.Contains(TelemetryValidationIssue.LongitudeOutOfRange, result.Issues);
        Assert.Contains(TelemetryValidationIssue.TrackOutOfRange, result.Issues);
    }

    [Fact]
    public void Validate_DoesNotMutateTelemetryPoint()
    {
        var point = CreatePoint(latitude: 28.5, longitude: -81.3, trackDegrees: 120);
        var original = new
        {
            point.Timestamp,
            point.Icao24,
            point.Callsign,
            point.Latitude,
            point.Longitude,
            point.AltitudeFeet,
            point.GroundSpeedKnots,
            point.TrackDegrees,
            point.VerticalRateFeetPerMinute
        };

        _validator.Validate(point);

        Assert.Equal(original.Timestamp, point.Timestamp);
        Assert.Equal(original.Icao24, point.Icao24);
        Assert.Equal(original.Callsign, point.Callsign);
        Assert.Equal(original.Latitude, point.Latitude);
        Assert.Equal(original.Longitude, point.Longitude);
        Assert.Equal(original.AltitudeFeet, point.AltitudeFeet);
        Assert.Equal(original.GroundSpeedKnots, point.GroundSpeedKnots);
        Assert.Equal(original.TrackDegrees, point.TrackDegrees);
        Assert.Equal(original.VerticalRateFeetPerMinute, point.VerticalRateFeetPerMinute);
    }

    private static FlightTelemetryPoint CreatePoint(
        DateTimeOffset? timestamp = null,
        double? latitude = 28.5,
        double? longitude = -81.3,
        double? altitudeFeet = 8_000,
        double? groundSpeedKnots = 220,
        double? trackDegrees = 120,
        double? verticalRateFeetPerMinute = 500) => new()
    {
        Timestamp = timestamp ?? new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero),
        Icao24 = "abc123",
        Callsign = "FIP123",
        Latitude = latitude,
        Longitude = longitude,
        AltitudeFeet = altitudeFeet,
        GroundSpeedKnots = groundSpeedKnots,
        TrackDegrees = trackDegrees,
        VerticalRateFeetPerMinute = verticalRateFeetPerMinute
    };
}
