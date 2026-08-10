using Fip.Application.Flights;
using Fip.Application.Telemetry;
using Fip.Domain.FlightEvents;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Tests;

public sealed class SyntheticTrajectoryDetectionTests
{
    private readonly TakeoffDetector _takeoffDetector = new(new TelemetryPointValidator());
    private readonly LandingDetector _landingDetector = new(new TelemetryPointValidator());

    [Fact]
    public void Takeoff_NormalTaxiAccelerationAndClimbProducesOneEvent()
    {
        var telemetry = new SyntheticTelemetryBuilder()
            .WithSamplingInterval(TimeSpan.FromSeconds(1))
            .AtSample(0, 1_000, 10, 0)
            .AtSample(1, 1_005, 30, 50)
            .AtSample(2, 1_010, 65, 150)
            .AtSample(3, 1_200, 100, 800)
            .AtSample(4, 1_500, 120, 1_000)
            .AtSample(5, 1_900, 140, 1_000)
            .AtSample(6, 2_300, 160, 1_000)
            .Build();

        var result = _takeoffDetector.Detect(telemetry);

        Assert.NotNull(result);
        Assert.Equal(FlightEventType.Takeoff, result.Type);
        Assert.Equal(telemetry[3].Timestamp, result.Timestamp);
    }

    [Fact]
    public void Takeoff_TaxiNoiseDoesNotProduceAnEvent()
    {
        var telemetry = new SyntheticTelemetryBuilder()
            .AtSample(0, 1_000, 5, 0)
            .AtSample(1, 1_015, 20, 400)
            .AtSample(2, 995, 40, 0)
            .AtSample(3, 1_020, 60, 300)
            .AtSample(4, 1_005, 65, 0)
            .AtSample(5, 1_025, 70, 200)
            .AtSample(6, 1_010, 75, 0)
            .Build();

        Assert.Null(_takeoffDetector.Detect(telemetry));
    }

    [Fact]
    public void Takeoff_TemporaryAltitudeSpikeDoesNotProduceAnEvent()
    {
        var telemetry = new SyntheticTelemetryBuilder()
            .AtSample(0, 1_000, 10, 0)
            .AtSample(1, 1_010, 30, 0)
            .AtSample(2, 1_500, 55, 2_000)
            .AtSample(3, 1_015, 65, 0)
            .AtSample(4, 1_005, 70, 0)
            .AtSample(5, 1_020, 75, 0)
            .AtSample(6, 1_010, 70, 0)
            .Build();

        Assert.Null(_takeoffDetector.Detect(telemetry));
    }

    [Fact]
    public void Takeoff_MissingTelemetryAroundClimbStillDetectsWhenPostEvidenceIsSufficient()
    {
        var telemetry = new SyntheticTelemetryBuilder()
            .AtSample(0, 1_000, 10, 0)
            .AtSample(1, 1_005, 30, 50)
            .AtSample(2, 1_010, 65, 150)
            .AtOffset(TimeSpan.FromSeconds(42), 1_200, 100, 800)
            .AtOffset(TimeSpan.FromSeconds(43), 1_500, 120, 1_000)
            .AtOffset(TimeSpan.FromSeconds(44), 1_900, 140, 1_000)
            .AtOffset(TimeSpan.FromSeconds(45), 2_300, 160, 1_000)
            .Build();

        var result = _takeoffDetector.Detect(telemetry);

        Assert.NotNull(result);
        Assert.Equal(telemetry[3].Timestamp, result.Timestamp);
    }

    [Fact]
    public void Takeoff_GapAtTransitionWithInsufficientPostEvidenceIsConservative()
    {
        var telemetry = new SyntheticTelemetryBuilder()
            .AtSample(0, 1_000, 10, 0)
            .AtSample(1, 1_005, 30, 50)
            .AtSample(2, 1_010, 65, 150)
            .AtOffset(TimeSpan.FromSeconds(42), 1_200, 100, 800)
            .AtOffset(TimeSpan.FromSeconds(43), 1_500, 120, 1_000)
            .Build();

        Assert.Null(_takeoffDetector.Detect(telemetry));
    }

    [Fact]
    public void Takeoff_AlreadyAirborneTrajectoryDoesNotInventAnEvent()
    {
        var telemetry = new SyntheticTelemetryBuilder()
            .AtSample(0, 10_000, 220, 1_000)
            .AtSample(1, 10_300, 230, 1_000)
            .AtSample(2, 10_700, 240, 1_000)
            .AtSample(3, 11_100, 250, 1_000)
            .AtSample(4, 11_500, 260, 1_000)
            .AtSample(5, 11_900, 270, 1_000)
            .Build();

        Assert.Null(_takeoffDetector.Detect(telemetry));
    }

    [Fact]
    public void Landing_NormalDescentDecelerationAndRolloutProducesOneEvent()
    {
        var telemetry = new SyntheticTelemetryBuilder()
            .AtSample(0, 5_000, 220, -800)
            .AtSample(1, 4_300, 200, -700)
            .AtSample(2, 3_600, 180, -700)
            .AtSample(3, 2_900, 160, -600)
            .AtSample(4, 2_850, 140, -100)
            .AtSample(5, 2_900, 120, 0)
            .AtSample(6, 2_880, 90, 0)
            .AtSample(7, 2_860, 60, 0)
            .Build();

        var result = _landingDetector.Detect(telemetry);

        Assert.NotNull(result);
        Assert.Equal(FlightEventType.Landing, result.Type);
        Assert.Equal(telemetry[4].Timestamp, result.Timestamp);
    }

    [Fact]
    public void Landing_TemporaryLevelOffWithContinuingApproachDoesNotProduceAnEvent()
    {
        var telemetry = new SyntheticTelemetryBuilder()
            .AtSample(0, 5_000, 220, -800)
            .AtSample(1, 4_300, 200, -700)
            .AtSample(2, 3_600, 180, -700)
            .AtSample(3, 3_000, 160, -600)
            .AtSample(4, 3_000, 150, 0)
            .AtSample(5, 3_020, 140, 0)
            .AtSample(6, 2_400, 130, -600)
            .AtSample(7, 1_800, 120, -500)
            .Build();

        Assert.Null(_landingDetector.Detect(telemetry));
    }

    [Fact]
    public void Landing_FinalApproachNoiseStillProducesLanding()
    {
        var telemetry = new SyntheticTelemetryBuilder()
            .AtSample(0, 5_000, 220, -800)
            .AtSample(1, 4_300, 200, -700)
            .AtSample(2, 4_350, 180, -700)
            .AtSample(3, 3_600, 160, -600)
            .AtSample(4, 3_550, 140, -100)
            .AtSample(5, 3_600, 120, 0)
            .AtSample(6, 3_580, 90, 0)
            .AtSample(7, 3_560, 60, 0)
            .Build();

        var result = _landingDetector.Detect(telemetry);

        Assert.NotNull(result);
        Assert.Equal(FlightEventType.Landing, result.Type);
    }

    [Fact]
    public void Landing_IncompleteApproachDoesNotInventAnEvent()
    {
        var telemetry = new SyntheticTelemetryBuilder()
            .AtSample(0, 5_000, 220, -800)
            .AtSample(1, 4_300, 200, -700)
            .AtSample(2, 3_600, 180, -700)
            .AtSample(3, 2_900, 160, -600)
            .AtSample(4, 2_300, 150, -600)
            .AtSample(5, 1_700, 140, -500)
            .AtSample(6, 1_100, 130, -500)
            .Build();

        Assert.Null(_landingDetector.Detect(telemetry));
    }

    [Fact]
    public void Landing_AlreadyGroundTrajectoryDoesNotInventAnEvent()
    {
        var telemetry = new SyntheticTelemetryBuilder()
            .AtSample(0, 2_000, 10, 0)
            .AtSample(1, 2_010, 20, 0)
            .AtSample(2, 2_000, 30, 0)
            .AtSample(3, 2_020, 40, 0)
            .AtSample(4, 2_010, 50, 0)
            .AtSample(5, 2_000, 60, 0)
            .AtSample(6, 2_010, 70, 0)
            .AtSample(7, 2_000, 80, 0)
            .Build();

        Assert.Null(_landingDetector.Detect(telemetry));
    }

    [Fact]
    public void Landing_GoAroundLikeTrajectoryDoesNotProduceLanding()
    {
        var telemetry = new SyntheticTelemetryBuilder()
            .AtSample(0, 5_000, 220, -800)
            .AtSample(1, 4_300, 200, -700)
            .AtSample(2, 3_600, 180, -700)
            .AtSample(3, 3_000, 160, -600)
            .AtSample(4, 2_950, 150, -100)
            .AtSample(5, 3_300, 150, 800)
            .AtSample(6, 3_700, 160, 800)
            .AtSample(7, 4_100, 170, 800)
            .AtSample(8, 4_500, 180, 800)
            .Build();

        Assert.Null(_landingDetector.Detect(telemetry));
    }

    [Fact]
    public void Landing_GoAroundFollowedByActualLandingProducesOneFinalEvent()
    {
        var telemetry = new SyntheticTelemetryBuilder()
            .AtSample(0, 5_000, 220, -800)
            .AtSample(1, 4_300, 200, -700)
            .AtSample(2, 3_600, 180, -700)
            .AtSample(3, 3_000, 160, -600)
            .AtSample(4, 2_950, 150, -100)
            .AtSample(5, 3_300, 155, 800)
            .AtSample(6, 3_700, 160, 800)
            .AtSample(7, 4_100, 170, 800)
            .AtSample(8, 4_500, 180, 800)
            .AtSample(9, 3_800, 180, -700)
            .AtSample(10, 3_200, 170, -700)
            .AtSample(11, 2_600, 160, -700)
            .AtSample(12, 2_000, 150, -600)
            .AtSample(13, 1_950, 140, -100)
            .AtSample(14, 1_960, 120, 0)
            .AtSample(15, 1_940, 120, 0)
            .AtSample(16, 1_930, 60, 0)
            .Build();

        var result = _landingDetector.Detect(telemetry);

        Assert.NotNull(result);
        Assert.Equal(FlightEventType.Landing, result.Type);
        Assert.Equal(telemetry[13].Timestamp, result.Timestamp);
    }

    [Fact]
    public void Detectors_HandleUnorderedTimestamps()
    {
        var takeoff = new SyntheticTelemetryBuilder()
            .AtSample(0, 1_000, 10, 0)
            .AtSample(1, 1_010, 40, 100)
            .AtSample(2, 1_020, 70, 200)
            .AtSample(3, 1_200, 100, 800)
            .AtSample(4, 1_500, 120, 1_000)
            .AtSample(5, 1_900, 140, 1_000)
            .AtSample(6, 2_300, 160, 1_000)
            .Build()
            .Reverse()
            .ToArray();
        var landing = new SyntheticTelemetryBuilder()
            .AtSample(0, 5_000, 220, -800)
            .AtSample(1, 4_300, 200, -700)
            .AtSample(2, 3_600, 180, -700)
            .AtSample(3, 2_900, 160, -600)
            .AtSample(4, 2_850, 140, -100)
            .AtSample(5, 2_900, 120, 0)
            .AtSample(6, 2_880, 90, 0)
            .AtSample(7, 2_860, 60, 0)
            .Build()
            .Reverse()
            .ToArray();

        Assert.NotNull(_takeoffDetector.Detect(takeoff));
        Assert.NotNull(_landingDetector.Detect(landing));
    }

    [Fact]
    public void Detectors_TolerateDuplicateTimestamps()
    {
        var telemetry = new SyntheticTelemetryBuilder()
            .AtSample(0, 1_000, 10, 0)
            .AtOffset(TimeSpan.Zero, 1_005, 30, 50)
            .AtSample(1, 1_010, 65, 150)
            .AtSample(2, 1_200, 100, 800)
            .AtSample(3, 1_500, 120, 1_000)
            .AtSample(4, 1_900, 140, 1_000)
            .AtSample(5, 2_300, 160, 1_000)
            .Build();

        var result = _takeoffDetector.Detect(telemetry);

        Assert.NotNull(result);
        Assert.Equal(FlightEventType.Takeoff, result.Type);
    }

    [Fact]
    public void Detectors_IgnoreInvalidPointsUsingExistingValidation()
    {
        var telemetry = new SyntheticTelemetryBuilder()
            .AtSample(0, 1_000, 10, 0)
            .AtSample(1, 1_010, 40, 100, latitude: 95)
            .AtSample(2, 1_020, 70, 200)
            .AtSample(3, 1_200, 100, 800)
            .AtSample(4, 1_500, 120, 1_000)
            .AtSample(5, 1_900, 140, 1_000)
            .AtSample(6, 2_300, 160, 1_000)
            .Build();

        var result = _takeoffDetector.Detect(telemetry);

        Assert.NotNull(result);
        Assert.Equal(FlightEventType.Takeoff, result.Type);
    }

    [Fact]
    public void Takeoff_GapAwayFromTransitionDoesNotChangeDetectedEvent()
    {
        var builder = new SyntheticTelemetryBuilder()
            .AtSample(0, 1_000, 10, 0)
            .AtSample(1, 1_010, 40, 100)
            .AtSample(2, 1_020, 70, 200)
            .AtSample(3, 1_200, 100, 800)
            .AtSample(4, 1_500, 120, 1_000)
            .AtSample(5, 1_900, 140, 1_000)
            .AtSample(6, 2_300, 160, 1_000)
            .AtOffset(TimeSpan.FromSeconds(60), 5_000, 220, 500)
            .AtOffset(TimeSpan.FromSeconds(61), 5_100, 220, 500);

        var result = _takeoffDetector.Detect(builder.Build());

        Assert.NotNull(result);
        Assert.Equal(builder.Build()[3].Timestamp, result.Timestamp);
    }
}
