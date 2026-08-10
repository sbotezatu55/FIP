using Fip.Application.Flights;
using Fip.Application.Telemetry;
using Fip.Domain.FlightEvents;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Tests;

public sealed class LandingDetectorTests
{
    private readonly LandingDetector _detector = new(new TelemetryPointValidator());

    [Fact]
    public void Detect_ReturnsLandingForDescentAndRolloutTransition()
    {
        var points = new[]
        {
            CreatePoint(0, 220, 5_000, -800),
            CreatePoint(10, 200, 4_300, -700),
            CreatePoint(20, 180, 3_600, -700),
            CreatePoint(30, 160, 2_900, -600),
            CreatePoint(40, 140, 2_850, -100),
            CreatePoint(50, 120, 2_900, 0),
            CreatePoint(60, 90, 2_880, 0),
            CreatePoint(70, 60, 2_860, 0)
        };

        var result = _detector.Detect(points);

        Assert.NotNull(result);
        Assert.Equal(FlightEventType.Landing, result.Type);
        Assert.Equal(CreateTimestamp(40), result.Timestamp);
        Assert.Same(points[4], result.TelemetryPoint);
    }

    [Fact]
    public void Detect_ToleratesNoisyAltitudeSamplesDuringApproach()
    {
        var points = new[]
        {
            CreatePoint(0, 220, 5_000, -800),
            CreatePoint(10, 200, 4_300, -700),
            CreatePoint(20, 180, 4_350, -700),
            CreatePoint(30, 160, 3_600, -600),
            CreatePoint(40, 140, 3_550, -100),
            CreatePoint(50, 120, 3_600, 0),
            CreatePoint(60, 90, 3_580, 0),
            CreatePoint(70, 60, 3_560, 0)
        };

        var result = _detector.Detect(points);

        Assert.NotNull(result);
        Assert.Equal(FlightEventType.Landing, result.Type);
    }

    [Fact]
    public void Detect_DoesNotClassifyTemporaryLevelOffAsLanding()
    {
        var points = new[]
        {
            CreatePoint(0, 220, 5_000, -800),
            CreatePoint(10, 200, 4_300, -700),
            CreatePoint(20, 180, 3_600, -700),
            CreatePoint(30, 160, 3_000, -600),
            CreatePoint(40, 150, 3_000, 0),
            CreatePoint(50, 140, 3_020, 0),
            CreatePoint(60, 130, 3_000, 0),
            CreatePoint(70, 120, 3_030, 0)
        };

        Assert.Null(_detector.Detect(points));
    }

    [Fact]
    public void Detect_DoesNotClassifyIncompleteDescendingTrajectory()
    {
        var points = new[]
        {
            CreatePoint(0, 220, 5_000, -800),
            CreatePoint(10, 200, 4_300, -700),
            CreatePoint(20, 180, 3_600, -700),
            CreatePoint(30, 160, 2_900, -600),
            CreatePoint(40, 150, 2_300, -600),
            CreatePoint(50, 140, 1_700, -500),
            CreatePoint(60, 130, 1_100, -500),
            CreatePoint(70, 120, 500, -400)
        };

        Assert.Null(_detector.Detect(points));
    }

    [Fact]
    public void Detect_DoesNotInventLandingForAlreadyOnGroundTelemetry()
    {
        var points = new[]
        {
            CreatePoint(0, 10, 2_000, 0),
            CreatePoint(10, 20, 2_010, 0),
            CreatePoint(20, 30, 2_000, 0),
            CreatePoint(30, 40, 2_020, 0),
            CreatePoint(40, 50, 2_010, 0),
            CreatePoint(50, 60, 2_000, 0),
            CreatePoint(60, 70, 2_010, 0),
            CreatePoint(70, 80, 2_000, 0)
        };

        Assert.Null(_detector.Detect(points));
    }

    [Fact]
    public void Detect_HandlesUnorderedTelemetry()
    {
        var landingPoint = CreatePoint(40, 140, 2_850, -100);
        var points = new[]
        {
            CreatePoint(70, 60, 2_860, 0),
            CreatePoint(20, 180, 3_600, -700),
            CreatePoint(60, 90, 2_880, 0),
            landingPoint,
            CreatePoint(0, 220, 5_000, -800),
            CreatePoint(50, 120, 2_900, 0),
            CreatePoint(30, 160, 2_900, -600),
            CreatePoint(10, 200, 4_300, -700)
        };

        var result = _detector.Detect(points);

        Assert.NotNull(result);
        Assert.Equal(landingPoint.Timestamp, result.Timestamp);
        Assert.Equal(FlightEventType.Landing, result.Type);
    }

    [Fact]
    public void Detect_RejectsDescentFollowedBySustainedClimb()
    {
        var points = new[]
        {
            CreatePoint(0, 220, 5_000, -800),
            CreatePoint(10, 200, 4_300, -700),
            CreatePoint(20, 180, 3_600, -700),
            CreatePoint(30, 160, 3_000, -600),
            CreatePoint(40, 150, 2_950, -100),
            CreatePoint(50, 150, 3_300, 800),
            CreatePoint(60, 160, 3_700, 800),
            CreatePoint(70, 170, 4_100, 800),
            CreatePoint(80, 180, 4_500, 800)
        };

        Assert.Null(_detector.Detect(points));
    }

    private static FlightTelemetryPoint CreatePoint(
        int seconds,
        double groundspeedKnots,
        double altitudeFeet,
        double verticalRateFeetPerMinute) => new()
    {
        Timestamp = CreateTimestamp(seconds),
        Icao24 = "abc123",
        Callsign = "FIP123",
        GroundSpeedKnots = groundspeedKnots,
        AltitudeFeet = altitudeFeet,
        VerticalRateFeetPerMinute = verticalRateFeetPerMinute
    };

    private static DateTimeOffset CreateTimestamp(int seconds) =>
        new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero).AddSeconds(seconds);
}
