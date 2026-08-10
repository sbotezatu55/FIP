using Fip.Application.Flights;
using Fip.Application.Telemetry;
using Fip.Domain.FlightEvents;

namespace Fip.Application.Tests;

public sealed class TopOfClimbDetectorTests
{
    private readonly TopOfClimbDetector _detector = new(new TelemetryPointValidator());

    [Fact]
    public void Detect_ReturnsTopOfClimb_WhenSustainedClimbTransitionsToCruise()
    {
        var telemetry = CreateBuilder()
            .AtSample(0, 10_000, 220, 1_000)
            .AtSample(1, 11_000, 230, 900)
            .AtSample(2, 12_000, 240, 800)
            .AtSample(3, 13_000, 250, 700)
            .AtSample(4, 13_050, 450, 100)
            .AtSample(5, 13_000, 450, 0)
            .AtSample(6, 13_025, 450, 50)
            .AtSample(7, 13_000, 450, 0)
            .AtSample(8, 13_010, 450, 0)
            .Build();

        var result = _detector.Detect(telemetry);

        Assert.NotNull(result);
        Assert.Equal(FlightEventType.TopOfClimb, result.Type);
        Assert.Equal(telemetry[4].Timestamp, result.Timestamp);
        Assert.Same(telemetry[4], result.TelemetryPoint);
    }

    [Fact]
    public void Detect_DoesNotReturnTemporaryLevelOffAsTopOfClimb()
    {
        var telemetry = CreateBuilder()
            .AtSample(0, 10_000, 220, 1_000)
            .AtSample(1, 11_000, 230, 900)
            .AtSample(2, 12_000, 240, 800)
            .AtSample(3, 13_000, 250, 700)
            .AtSample(4, 13_000, 450, 0)
            .AtSample(5, 13_020, 450, 0)
            .AtSample(6, 14_000, 260, 800)
            .AtSample(7, 15_000, 270, 800)
            .AtSample(8, 16_000, 280, 800)
            .AtSample(9, 16_050, 450, 0)
            .AtSample(10, 16_000, 450, 0)
            .AtSample(11, 16_025, 450, 50)
            .AtSample(12, 16_000, 450, 0)
            .AtSample(13, 16_010, 450, 0)
            .Build();

        var result = _detector.Detect(telemetry);

        Assert.NotNull(result);
        Assert.Equal(telemetry[9].Timestamp, result.Timestamp);
    }

    [Fact]
    public void Detect_DoesNotReturnTopOfClimbForSingleNoisyVerticalRateSample()
    {
        var telemetry = CreateBuilder()
            .AtSample(0, 30_000, 450, 0)
            .AtSample(1, 30_025, 450, 1_000)
            .AtSample(2, 30_000, 450, 0)
            .AtSample(3, 30_020, 450, 0)
            .AtSample(4, 30_000, 450, 0)
            .AtSample(5, 30_010, 450, 0)
            .Build();

        Assert.Null(_detector.Detect(telemetry));
    }

    [Fact]
    public void Detect_HandlesAltitudeNoiseDuringCruise()
    {
        var telemetry = CreateBuilder()
            .AtSample(0, 10_000, 220, 1_000)
            .AtSample(1, 11_000, 230, 900)
            .AtSample(2, 12_000, 240, 800)
            .AtSample(3, 13_000, 250, 700)
            .AtSample(4, 13_300, 450, 100)
            .AtSample(5, 13_050, 450, -100)
            .AtSample(6, 13_250, 450, 100)
            .AtSample(7, 13_100, 450, -50)
            .AtSample(8, 13_200, 450, 0)
            .Build();

        var result = _detector.Detect(telemetry);

        Assert.NotNull(result);
        Assert.Equal(telemetry[4].Timestamp, result.Timestamp);
    }

    [Fact]
    public void Detect_ReturnsNull_WhenFlightBeginsAlreadyInCruise()
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
    public void Detect_ReturnsNull_WhenFlightEndsDuringClimb()
    {
        var telemetry = CreateBuilder()
            .AtSample(0, 10_000, 220, 1_000)
            .AtSample(1, 11_000, 230, 900)
            .AtSample(2, 12_000, 240, 800)
            .AtSample(3, 13_000, 250, 700)
            .AtSample(4, 14_000, 260, 700)
            .AtSample(5, 15_000, 270, 700)
            .Build();

        Assert.Null(_detector.Detect(telemetry));
    }

    [Fact]
    public void Detect_ReturnsNull_WhenClimbIsFollowedImmediatelyByDescent()
    {
        var telemetry = CreateBuilder()
            .AtSample(0, 10_000, 220, 1_000)
            .AtSample(1, 11_000, 230, 900)
            .AtSample(2, 12_000, 240, 800)
            .AtSample(3, 13_000, 250, 700)
            .AtSample(4, 12_500, 240, -500)
            .AtSample(5, 11_500, 230, -500)
            .AtSample(6, 10_500, 220, -500)
            .AtSample(7, 9_500, 210, -500)
            .AtSample(8, 8_500, 200, -500)
            .Build();

        Assert.Null(_detector.Detect(telemetry));
    }

    [Fact]
    public void Detect_HandlesUnorderedTelemetry()
    {
        var telemetry = CreateBuilder()
            .AtSample(0, 10_000, 220, 1_000)
            .AtSample(1, 11_000, 230, 900)
            .AtSample(2, 12_000, 240, 800)
            .AtSample(3, 13_000, 250, 700)
            .AtSample(4, 13_050, 450, 100)
            .AtSample(5, 13_000, 450, 0)
            .AtSample(6, 13_025, 450, 50)
            .AtSample(7, 13_000, 450, 0)
            .AtSample(8, 13_010, 450, 0)
            .Build()
            .Reverse()
            .ToArray();

        var result = _detector.Detect(telemetry);

        Assert.NotNull(result);
        Assert.Equal(telemetry.Min(point => point.Timestamp).AddSeconds(120), result.Timestamp);
    }

    [Fact]
    public void Detect_ReturnsNull_WhenGapOccursAroundCandidate()
    {
        var telemetry = CreateBuilder()
            .AtSample(0, 10_000, 220, 1_000)
            .AtSample(1, 11_000, 230, 900)
            .AtSample(2, 12_000, 240, 800)
            .AtSample(3, 13_000, 250, 700)
            .AtOffset(TimeSpan.FromSeconds(300), 13_050, 450, 100)
            .AtOffset(TimeSpan.FromSeconds(330), 13_000, 450, 0)
            .AtOffset(TimeSpan.FromSeconds(360), 13_025, 450, 50)
            .AtOffset(TimeSpan.FromSeconds(390), 13_000, 450, 0)
            .AtOffset(TimeSpan.FromSeconds(420), 13_010, 450, 0)
            .Build();

        Assert.Null(_detector.Detect(telemetry));
    }

    [Fact]
    public void Detect_ReturnsFirstTopOfClimb_AfterInitialClimbAndBeforeStepClimb()
    {
        var telemetry = CreateBuilder()
            .AtSample(0, 10_000, 220, 1_000)
            .AtSample(1, 11_000, 230, 900)
            .AtSample(2, 12_000, 240, 800)
            .AtSample(3, 13_000, 250, 700)
            .AtSample(4, 13_050, 450, 100)
            .AtSample(5, 13_000, 450, 0)
            .AtSample(6, 13_025, 450, 50)
            .AtSample(7, 13_000, 450, 0)
            .AtSample(8, 13_010, 450, 0)
            .AtSample(9, 14_000, 260, 800)
            .AtSample(10, 15_000, 270, 800)
            .AtSample(11, 15_050, 450, 0)
            .AtSample(12, 15_000, 450, 0)
            .AtSample(13, 15_025, 450, 0)
            .AtSample(14, 15_000, 450, 0)
            .AtSample(15, 15_010, 450, 0)
            .Build();

        var result = _detector.Detect(telemetry);

        Assert.NotNull(result);
        Assert.Equal(telemetry[4].Timestamp, result.Timestamp);
    }

    [Fact]
    public void Detect_IgnoresInvalidPointsUsingExistingValidation()
    {
        var telemetry = CreateBuilder()
            .AtSample(0, 10_000, 220, 1_000)
            .AtSample(1, 11_000, 230, 900, latitude: 95)
            .AtSample(2, 12_000, 240, 800)
            .AtSample(3, 13_000, 250, 700)
            .AtSample(4, 13_050, 450, 100)
            .AtSample(5, 13_000, 450, 0)
            .AtSample(6, 13_025, 450, 50)
            .AtSample(7, 13_000, 450, 0)
            .AtSample(8, 13_010, 450, 0)
            .AtSample(9, 13_000, 450, 0)
            .Build();

        var result = _detector.Detect(telemetry);

        Assert.NotNull(result);
        Assert.Equal(telemetry[5].Timestamp, result.Timestamp);
    }

    private static SyntheticTelemetryBuilder CreateBuilder() =>
        new SyntheticTelemetryBuilder().WithSamplingInterval(TimeSpan.FromSeconds(30));
}
