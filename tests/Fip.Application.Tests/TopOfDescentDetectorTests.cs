using Fip.Application.Flights;
using Fip.Application.Telemetry;
using Fip.Domain.FlightEvents;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Tests;

public sealed class TopOfDescentDetectorTests
{
    private readonly TopOfDescentDetector _detector = new(new TelemetryPointValidator());

    [Fact]
    public void Detect_ReturnsTopOfDescent_WhenCruiseTransitionsToSustainedDescent()
    {
        var telemetry = CreateBuilder()
            .AtSample(0, 30_000, 450, 0)
            .AtSample(1, 30_050, 450, 0)
            .AtSample(2, 30_000, 450, 50)
            .AtSample(3, 30_025, 450, 0)
            .AtSample(4, 29_500, 440, -300)
            .AtSample(5, 28_500, 430, -500)
            .AtSample(6, 27_500, 420, -500)
            .AtSample(7, 26_500, 410, -500)
            .AtSample(8, 25_000, 400, -500)
            .Build();

        var result = _detector.Detect(telemetry);

        Assert.NotNull(result);
        Assert.Equal(FlightEventType.TopOfDescent, result.Type);
        Assert.Equal(telemetry[4].Timestamp, result.Timestamp);
        Assert.Same(telemetry[4], result.TelemetryPoint);
    }

    [Fact]
    public void Detect_ReturnsNull_ForSingleNegativeVerticalRateSpike()
    {
        var telemetry = CreateBuilder()
            .AtSample(0, 30_000, 450, 0)
            .AtSample(1, 30_050, 450, 0)
            .AtSample(2, 30_000, 450, 0)
            .AtSample(3, 30_025, 450, 0)
            .AtSample(4, 29_700, 440, -500)
            .AtSample(5, 30_000, 440, 0)
            .AtSample(6, 30_025, 440, 0)
            .AtSample(7, 30_000, 440, 0)
            .AtSample(8, 30_025, 440, 0)
            .Build();

        Assert.Null(_detector.Detect(telemetry));
    }

    [Fact]
    public void Detect_ReturnsNull_ForMinorCruiseAltitudeCorrection()
    {
        var telemetry = CreateBuilder()
            .AtSample(0, 30_000, 450, 0)
            .AtSample(1, 30_050, 450, 0)
            .AtSample(2, 30_000, 450, 0)
            .AtSample(3, 30_025, 450, 0)
            .AtSample(4, 29_600, 440, -300)
            .AtSample(5, 30_000, 440, 200)
            .AtSample(6, 30_025, 440, 0)
            .AtSample(7, 30_000, 440, 0)
            .AtSample(8, 30_025, 440, 0)
            .AtSample(9, 30_000, 440, 0)
            .Build();

        Assert.Null(_detector.Detect(telemetry));
    }

    [Fact]
    public void Detect_ReturnsLaterDescent_AfterTemporaryAltitudeCorrection()
    {
        var telemetry = CreateBuilder()
            .AtSample(0, 30_000, 450, 0)
            .AtSample(1, 30_050, 450, 0)
            .AtSample(2, 30_000, 450, 0)
            .AtSample(3, 30_025, 450, 0)
            .AtSample(4, 29_600, 440, -300)
            .AtSample(5, 30_000, 440, 200)
            .AtSample(6, 30_025, 440, 0)
            .AtSample(7, 30_000, 440, 0)
            .AtSample(8, 30_025, 440, 0)
            .AtSample(9, 29_500, 430, -300)
            .AtSample(10, 28_500, 420, -500)
            .AtSample(11, 27_500, 410, -500)
            .AtSample(12, 26_500, 400, -500)
            .AtSample(13, 25_000, 390, -500)
            .Build();

        var result = _detector.Detect(telemetry);

        Assert.NotNull(result);
        Assert.Equal(telemetry[9].Timestamp, result.Timestamp);
    }

    [Fact]
    public void Detect_ReturnsNull_WhenFlightBeginsAlreadyDescending()
    {
        var telemetry = CreateBuilder()
            .AtSample(0, 30_000, 450, -500)
            .AtSample(1, 29_000, 440, -500)
            .AtSample(2, 28_000, 430, -500)
            .AtSample(3, 27_000, 420, -500)
            .AtSample(4, 26_000, 410, -500)
            .AtSample(5, 25_000, 400, -500)
            .Build();

        Assert.Null(_detector.Detect(telemetry));
    }

    [Fact]
    public void Detect_ReturnsNull_WhenFlightEndsInCruise()
    {
        var telemetry = CreateBuilder()
            .AtSample(0, 30_000, 450, 0)
            .AtSample(1, 30_050, 450, 0)
            .AtSample(2, 30_000, 450, 0)
            .AtSample(3, 30_025, 450, 0)
            .AtSample(4, 30_000, 450, 0)
            .AtSample(5, 30_050, 450, 0)
            .Build();

        Assert.Null(_detector.Detect(telemetry));
    }

    [Fact]
    public void Detect_ReturnsNull_WhenClimbTransitionsDirectlyToDescent()
    {
        var telemetry = CreateBuilder()
            .AtSample(0, 25_000, 400, 800)
            .AtSample(1, 26_000, 410, 800)
            .AtSample(2, 27_000, 420, 800)
            .AtSample(3, 28_000, 430, 800)
            .AtSample(4, 27_000, 430, -500)
            .AtSample(5, 26_000, 420, -500)
            .AtSample(6, 25_000, 410, -500)
            .AtSample(7, 24_000, 400, -500)
            .AtSample(8, 23_000, 390, -500)
            .Build();

        Assert.Null(_detector.Detect(telemetry));
    }

    [Fact]
    public void Detect_HandlesUnorderedTelemetry()
    {
        var telemetry = CreateBuilder()
            .AtSample(0, 30_000, 450, 0)
            .AtSample(1, 30_050, 450, 0)
            .AtSample(2, 30_000, 450, 0)
            .AtSample(3, 30_025, 450, 0)
            .AtSample(4, 29_500, 440, -300)
            .AtSample(5, 28_500, 430, -500)
            .AtSample(6, 27_500, 420, -500)
            .AtSample(7, 26_500, 410, -500)
            .AtSample(8, 25_000, 400, -500)
            .Build()
            .Reverse()
            .ToArray();

        var result = _detector.Detect(telemetry);

        Assert.NotNull(result);
        Assert.Equal(telemetry.Min(point => point.Timestamp).AddSeconds(120), result.Timestamp);
    }

    [Fact]
    public void Detect_IgnoresNoisyAltitudeDuringCruise()
    {
        var telemetry = CreateBuilder()
            .AtSample(0, 30_000, 450, 50)
            .AtSample(1, 30_300, 450, -100)
            .AtSample(2, 30_050, 450, 80)
            .AtSample(3, 30_250, 450, -50)
            .AtSample(4, 29_500, 440, -300)
            .AtSample(5, 28_500, 430, -500)
            .AtSample(6, 27_500, 420, -500)
            .AtSample(7, 26_500, 410, -500)
            .AtSample(8, 25_000, 400, -500)
            .Build();

        var result = _detector.Detect(telemetry);

        Assert.NotNull(result);
        Assert.Equal(telemetry[4].Timestamp, result.Timestamp);
    }

    [Fact]
    public void Detect_ReturnsNull_WhenGapOccursAroundCandidate()
    {
        var telemetry = CreateBuilder()
            .AtSample(0, 30_000, 450, 0)
            .AtSample(1, 30_050, 450, 0)
            .AtSample(2, 30_000, 450, 0)
            .AtSample(3, 30_025, 450, 0)
            .AtOffset(TimeSpan.FromSeconds(300), 29_500, 440, -300)
            .AtOffset(TimeSpan.FromSeconds(330), 28_500, 430, -500)
            .AtOffset(TimeSpan.FromSeconds(360), 27_500, 420, -500)
            .AtOffset(TimeSpan.FromSeconds(390), 26_500, 410, -500)
            .AtOffset(TimeSpan.FromSeconds(420), 25_000, 400, -500)
            .Build();

        Assert.Null(_detector.Detect(telemetry));
    }

    [Fact]
    public void Detect_ReturnsTopOfDescent_AfterStepClimbAndFinalCruise()
    {
        var telemetry = CreateBuilder()
            .AtSample(0, 30_000, 450, 0)
            .AtSample(1, 30_050, 450, 0)
            .AtSample(2, 34_000, 450, 800)
            .AtSample(3, 34_050, 450, 0)
            .AtSample(4, 34_000, 450, 0)
            .AtSample(5, 34_025, 450, 0)
            .AtSample(6, 34_000, 450, 0)
            .AtSample(7, 33_500, 440, -300)
            .AtSample(8, 32_500, 430, -500)
            .AtSample(9, 31_500, 420, -500)
            .AtSample(10, 30_500, 410, -500)
            .AtSample(11, 29_000, 400, -500)
            .Build();

        var result = _detector.Detect(telemetry);

        Assert.NotNull(result);
        Assert.Equal(telemetry[7].Timestamp, result.Timestamp);
    }

    [Fact]
    public void Detect_ReturnsNull_WhenDescentIsAbortedByClimbBackToCruise()
    {
        var telemetry = CreateBuilder()
            .AtSample(0, 30_000, 450, 0)
            .AtSample(1, 30_050, 450, 0)
            .AtSample(2, 30_000, 450, 0)
            .AtSample(3, 30_025, 450, 0)
            .AtSample(4, 30_000, 450, 0)
            .AtSample(5, 29_500, 440, -300)
            .AtSample(6, 28_500, 430, -500)
            .AtSample(7, 27_500, 420, -500)
            .AtSample(8, 26_500, 410, -500)
            .AtSample(9, 25_000, 400, -500)
            .AtSample(9, 27_000, 410, 700)
            .AtSample(10, 29_000, 420, 700)
            .AtSample(11, 30_000, 430, 700)
            .Build();

        Assert.Null(_detector.Detect(telemetry));
    }

    [Fact]
    public void Detect_IgnoresInvalidPointsUsingExistingValidation()
    {
        var telemetry = CreateBuilder()
            .AtSample(0, 30_000, 450, 0)
            .AtSample(1, 30_050, 450, 0, latitude: 95)
            .AtSample(2, 30_000, 450, 0)
            .AtSample(3, 30_025, 450, 0)
            .AtSample(4, 30_000, 450, 0)
            .AtSample(5, 29_500, 440, -300)
            .AtSample(6, 28_500, 430, -500)
            .AtSample(7, 27_500, 420, -500)
            .AtSample(8, 26_500, 410, -500)
            .AtSample(9, 25_000, 400, -500)
            .Build();

        var result = _detector.Detect(telemetry);

        Assert.NotNull(result);
        Assert.Equal(telemetry[5].Timestamp, result.Timestamp);
    }

    private static SyntheticTelemetryBuilder CreateBuilder() =>
        new SyntheticTelemetryBuilder().WithSamplingInterval(TimeSpan.FromSeconds(30));
}
