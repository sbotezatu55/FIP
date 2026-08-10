using Fip.Application.Telemetry;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Tests;

public sealed class TelemetryGapDetectorTests
{
    private readonly TelemetryGapDetector _detector = new();

    [Fact]
    public void Detect_ReturnsNoGapsForEmptyCollection()
    {
        var gaps = _detector.Detect(Array.Empty<FlightTelemetryPoint>());

        Assert.Empty(gaps);
    }

    [Fact]
    public void Detect_ReturnsNoGapsForSinglePoint()
    {
        var gaps = _detector.Detect(new[] { CreatePoint(0) });

        Assert.Empty(gaps);
    }

    [Fact]
    public void Detect_ReturnsNoGapsForNormalTelemetry()
    {
        var points = new[]
        {
            CreatePoint(0),
            CreatePoint(1),
            CreatePoint(2),
            CreatePoint(3)
        };

        var gaps = _detector.Detect(points);

        Assert.Empty(gaps);
    }

    [Fact]
    public void Detect_ReportsGapStartEndAndDuration()
    {
        var points = new[]
        {
            CreatePoint(0),
            CreatePoint(1),
            CreatePoint(260)
        };

        var gaps = _detector.Detect(points);

        var gap = Assert.Single(gaps);
        Assert.Equal(CreateTimestamp(1), gap.StartTime);
        Assert.Equal(CreateTimestamp(260), gap.EndTime);
        Assert.Equal(TimeSpan.FromSeconds(259), gap.Duration);
    }

    [Fact]
    public void Detect_ReportsAllGaps()
    {
        var points = new[]
        {
            CreatePoint(0),
            CreatePoint(1),
            CreatePoint(100),
            CreatePoint(101),
            CreatePoint(200)
        };

        var gaps = _detector.Detect(points);

        Assert.Equal(2, gaps.Count);
        Assert.Equal((CreateTimestamp(1), CreateTimestamp(100)), (gaps[0].StartTime, gaps[0].EndTime));
        Assert.Equal((CreateTimestamp(101), CreateTimestamp(200)), (gaps[1].StartTime, gaps[1].EndTime));
    }

    [Fact]
    public void Detect_SortsTelemetryWithoutMutatingInput()
    {
        var late = CreatePoint(100);
        var early = CreatePoint(0);
        var points = new[] { late, early };

        var gaps = _detector.Detect(points);

        Assert.Equal(new[] { late, early }, points);
        var gap = Assert.Single(gaps);
        Assert.Equal(early.Timestamp, gap.StartTime);
        Assert.Equal(late.Timestamp, gap.EndTime);
    }

    [Fact]
    public void Detect_DoesNotReportGapAtThresholdBoundary()
    {
        var gaps = _detector.Detect(new[]
        {
            CreatePoint(0),
            CreatePoint(30)
        });

        Assert.Empty(gaps);
    }

    [Fact]
    public void Detect_ReportsGapJustBeyondThreshold()
    {
        var gaps = _detector.Detect(new[]
        {
            CreatePoint(0),
            CreatePoint(31)
        });

        Assert.Single(gaps);
    }

    [Fact]
    public void Detect_DoesNotReportDuplicateTimestampsAsGaps()
    {
        var points = new[]
        {
            CreatePoint(1),
            CreatePoint(1),
            CreatePoint(2)
        };

        var gaps = _detector.Detect(points);

        Assert.Empty(gaps);
    }

    [Fact]
    public void Detect_ReportsOneGapBetweenPointGroups()
    {
        var points = new[]
        {
            CreatePoint(0),
            CreatePoint(1),
            CreatePoint(2),
            CreatePoint(300),
            CreatePoint(301),
            CreatePoint(302)
        };

        var gaps = _detector.Detect(points);

        var gap = Assert.Single(gaps);
        Assert.Equal(CreateTimestamp(2), gap.StartTime);
        Assert.Equal(CreateTimestamp(300), gap.EndTime);
    }

    [Fact]
    public void Constructor_RejectsNonPositiveThreshold()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TelemetryGapDetector(TimeSpan.Zero));
    }

    private static FlightTelemetryPoint CreatePoint(int seconds) => new()
    {
        Timestamp = CreateTimestamp(seconds),
        Icao24 = "abc123"
    };

    private static DateTimeOffset CreateTimestamp(int seconds) =>
        new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero).AddSeconds(seconds);
}
