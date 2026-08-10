using Fip.Application.Flights;
using Fip.Application.Telemetry;
using Fip.Domain.FlightEvents;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Tests;

public sealed class TakeoffDetectorTests
{
    private readonly TakeoffDetector _detector = new(new TelemetryPointValidator());

    [Fact]
    public void Detect_ReturnsTakeoffForSustainedGroundspeedAndClimbTransition()
    {
        var points = new[]
        {
            CreatePoint(0, 10, 1_000, 0),
            CreatePoint(10, 30, 1_010, 100),
            CreatePoint(20, 70, 1_020, 200),
            CreatePoint(30, 100, 1_200, 800),
            CreatePoint(40, 120, 1_450, 1_000),
            CreatePoint(50, 140, 1_800, 1_000),
            CreatePoint(60, 160, 2_200, 1_000)
        };

        var result = _detector.Detect(points);

        Assert.NotNull(result);
        Assert.Equal(FlightEventType.Takeoff, result.Type);
        Assert.Equal(CreateTimestamp(30), result.Timestamp);
        Assert.Same(points[3], result.TelemetryPoint);
    }

    [Fact]
    public void Detect_IgnoresIsolatedPositiveVerticalRateSpike()
    {
        var points = new[]
        {
            CreatePoint(0, 10, 1_000, 0),
            CreatePoint(10, 30, 1_005, 0),
            CreatePoint(20, 60, 1_010, 0),
            CreatePoint(30, 100, 1_015, 2_000),
            CreatePoint(40, 105, 1_010, 0),
            CreatePoint(50, 110, 1_015, 0),
            CreatePoint(60, 115, 1_020, 0)
        };

        Assert.Null(_detector.Detect(points));
    }

    [Fact]
    public void Detect_IgnoresSmallAltitudeFluctuationsDuringTaxi()
    {
        var points = new[]
        {
            CreatePoint(0, 5, 1_000, 0),
            CreatePoint(10, 20, 1_010, 0),
            CreatePoint(20, 40, 995, 0),
            CreatePoint(30, 60, 1_015, 0),
            CreatePoint(40, 65, 1_005, 0),
            CreatePoint(50, 70, 1_020, 0),
            CreatePoint(60, 75, 1_010, 0)
        };

        Assert.Null(_detector.Detect(points));
    }

    [Fact]
    public void Detect_ReturnsOneEventForSustainedClimb()
    {
        var points = new[]
        {
            CreatePoint(0, 10, 1_000, 0),
            CreatePoint(10, 50, 1_010, 100),
            CreatePoint(20, 75, 1_020, 200),
            CreatePoint(30, 90, 1_200, 700),
            CreatePoint(40, 110, 1_500, 900),
            CreatePoint(50, 130, 1_900, 1_000),
            CreatePoint(60, 150, 2_300, 1_000),
            CreatePoint(70, 170, 2_700, 1_000)
        };

        var result = _detector.Detect(points);

        Assert.NotNull(result);
        Assert.Equal(FlightEventType.Takeoff, result.Type);
        Assert.Equal(CreateTimestamp(30), result.Timestamp);
    }

    [Fact]
    public void Detect_DoesNotInventTakeoffForAlreadyAirborneTelemetry()
    {
        var points = new[]
        {
            CreatePoint(0, 220, 10_000, 1_000),
            CreatePoint(10, 230, 10_200, 1_000),
            CreatePoint(20, 240, 10_500, 1_000),
            CreatePoint(30, 250, 10_900, 1_000),
            CreatePoint(40, 260, 11_300, 1_000),
            CreatePoint(50, 270, 11_700, 1_000)
        };

        Assert.Null(_detector.Detect(points));
    }

    [Fact]
    public void Detect_HandlesUnorderedTelemetry()
    {
        var early = CreatePoint(0, 10, 1_000, 0);
        var transition = CreatePoint(30, 100, 1_200, 800);
        var points = new[]
        {
            CreatePoint(60, 160, 2_200, 1_000),
            CreatePoint(40, 120, 1_450, 1_000),
            transition,
            CreatePoint(50, 140, 1_800, 1_000),
            early,
            CreatePoint(20, 70, 1_020, 200),
            CreatePoint(10, 30, 1_010, 100)
        };

        var result = _detector.Detect(points);

        Assert.NotNull(result);
        Assert.Equal(transition.Timestamp, result.Timestamp);
        Assert.Equal(FlightEventType.Takeoff, result.Type);
    }

    [Fact]
    public void Detect_ReturnsNoEventWhenTelemetryIsInsufficient()
    {
        var points = new[]
        {
            CreatePoint(0, 10, 1_000, 0),
            CreatePoint(10, 100, 1_200, 800),
            CreatePoint(20, 120, 1_500, 900)
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
