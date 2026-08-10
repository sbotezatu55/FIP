using Fip.Application.Flights;
using Fip.Application.Telemetry;
using Fip.Domain.FlightEvents;
using Fip.Domain.Flights.Phases;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Tests;

public sealed class FlightPhaseClassifierTests
{
    private readonly FlightPhaseClassifier _classifier = new(new TelemetryPointValidator());

    [Fact]
    public void Classify_ReturnsExpectedPhasesForCompleteFlight()
    {
        var telemetry = CreateCompleteFlight();
        var events = CreateAnchoredEvents(telemetry, 3, 12, 17, 26);

        var segments = _classifier.Classify(telemetry, events);

        Assert.Equal(
            new[]
            {
                FlightPhase.Ground,
                FlightPhase.TakeoffRoll,
                FlightPhase.InitialClimb,
                FlightPhase.Climb,
                FlightPhase.Cruise,
                FlightPhase.Descent,
                FlightPhase.Approach,
                FlightPhase.LandingRoll
            },
            segments.Select(segment => segment.Phase));
    }

    [Fact]
    public void Classify_DoesNotInventGroundOrTakeoffForAirborneFlight()
    {
        var telemetry = new SyntheticTelemetryBuilder()
            .WithSamplingInterval(TimeSpan.FromSeconds(30))
            .AtSample(0, 30_000, 450, 0)
            .AtSample(1, 30_020, 450, 0)
            .AtSample(2, 29_990, 450, 0)
            .AtSample(3, 30_010, 450, 0)
            .AtSample(4, 30_000, 450, 0)
            .Build();

        var segments = _classifier.Classify(telemetry, Array.Empty<FlightEvent>()).ToList();

        Assert.Equal(new[] { FlightPhase.Cruise }, segments.Select(segment => segment.Phase));
    }

    [Fact]
    public void Classify_DoesNotCreateLandingRollWhenLandingIsAbsent()
    {
        var telemetry = CreateCompleteFlight();
        var events = CreateAnchoredEvents(telemetry, 3, 12, 17);

        var segments = _classifier.Classify(telemetry, events);

        Assert.DoesNotContain(segments, segment => segment.Phase == FlightPhase.LandingRoll);
    }

    [Fact]
    public void Classify_KeepsTemporaryClimbLevelOffAsClimb()
    {
        var telemetry = new SyntheticTelemetryBuilder()
            .WithSamplingInterval(TimeSpan.FromSeconds(30))
            .AtSample(0, 10_000, 250, 700)
            .AtSample(1, 11_000, 260, 700)
            .AtSample(2, 11_000, 260, 0)
            .AtSample(3, 12_000, 270, 700)
            .AtSample(4, 13_000, 280, 700)
            .Build();

        var segments = _classifier.Classify(telemetry, Array.Empty<FlightEvent>());

        Assert.Equal(new[] { FlightPhase.Climb }, segments.Select(segment => segment.Phase));
    }

    [Fact]
    public void Classify_KeepsSmallCruiseAltitudeCorrectionAsCruise()
    {
        var telemetry = new SyntheticTelemetryBuilder()
            .WithSamplingInterval(TimeSpan.FromSeconds(30))
            .AtSample(0, 30_000, 450, 0)
            .AtSample(1, 30_100, 450, 100)
            .AtSample(2, 29_950, 450, -100)
            .AtSample(3, 30_050, 450, 0)
            .AtSample(4, 30_000, 450, 0)
            .Build();

        var segments = _classifier.Classify(telemetry, Array.Empty<FlightEvent>());

        Assert.Equal(new[] { FlightPhase.Cruise }, segments.Select(segment => segment.Phase));
    }

    [Fact]
    public void Classify_MarksPointAfterLargeTelemetryGapAsUnknown()
    {
        var builder = new SyntheticTelemetryBuilder()
            .WithSamplingInterval(TimeSpan.FromSeconds(30))
            .AtSample(0, 30_000, 450, 0)
            .AtSample(1, 30_020, 450, 0)
            .AtOffset(TimeSpan.FromMinutes(5), 30_010, 450, 0)
            .AtOffset(TimeSpan.FromMinutes(5.5), 30_000, 450, 0);

        var segments = _classifier.Classify(builder.Build(), Array.Empty<FlightEvent>());

        Assert.Contains(segments, segment => segment.Phase == FlightPhase.Unknown);
        Assert.Contains(segments, segment => segment.Phase == FlightPhase.Cruise);
    }

    [Fact]
    public void Classify_IgnoresInvalidPointsAndOrdersTelemetry()
    {
        var builder = new SyntheticTelemetryBuilder()
            .WithSamplingInterval(TimeSpan.FromSeconds(30))
            .AtSample(0, 30_000, 450, 0)
            .AtSample(1, 30_010, 450, 0, latitude: 95)
            .AtSample(2, 30_020, 450, 0)
            .AtSample(3, 30_000, 450, 0);
        var telemetry = builder.Build().Reverse().ToList();

        var segments = _classifier.Classify(telemetry, Array.Empty<FlightEvent>()).ToList();

        Assert.Single(segments);
        Assert.Equal(FlightPhase.Cruise, segments[0].Phase);
        Assert.True(segments[0].StartTimestamp <= segments[0].EndTimestamp);
    }

    [Fact]
    public void Classify_DoesNotInventLandingRollForGoAroundLikeTrajectory()
    {
        var telemetry = new SyntheticTelemetryBuilder()
            .WithSamplingInterval(TimeSpan.FromSeconds(30))
            .AtSample(0, 10_000, 250, -500)
            .AtSample(1, 9_000, 240, -500)
            .AtSample(2, 8_000, 230, -500)
            .AtSample(3, 8_100, 250, 700)
            .AtSample(4, 9_200, 270, 700)
            .AtSample(5, 10_500, 290, 700)
            .Build();

        var segments = _classifier.Classify(telemetry, Array.Empty<FlightEvent>());

        Assert.DoesNotContain(segments, segment => segment.Phase == FlightPhase.LandingRoll);
    }

    private static IReadOnlyList<FlightTelemetryPoint> CreateCompleteFlight()
    {
        return new SyntheticTelemetryBuilder()
            .WithSamplingInterval(TimeSpan.FromSeconds(30))
            .AtSample(0, 1_000, 10, 0)
            .AtSample(1, 1_000, 30, 0)
            .AtSample(2, 1_000, 70, 0)
            .AtSample(3, 1_200, 100, 800)
            .AtSample(4, 2_000, 140, 800)
            .AtSample(5, 3_000, 180, 800)
            .AtSample(6, 4_000, 220, 800)
            .AtSample(7, 5_000, 250, 800)
            .AtSample(8, 6_000, 300, 500)
            .AtSample(9, 7_000, 350, 400)
            .AtSample(10, 10_000, 400, 300)
            .AtSample(11, 12_000, 450, 200)
            .AtSample(12, 12_050, 450, 0)
            .AtSample(13, 12_030, 450, 0)
            .AtSample(14, 12_000, 450, 0)
            .AtSample(15, 12_020, 450, 0)
            .AtSample(16, 12_000, 450, 0)
            .AtSample(17, 11_500, 440, -300)
            .AtSample(18, 10_500, 430, -400)
            .AtSample(19, 9_500, 420, -400)
            .AtSample(20, 8_500, 410, -400)
            .AtSample(21, 7_500, 400, -400)
            .AtSample(22, 6_500, 380, -400)
            .AtSample(23, 5_500, 350, -400)
            .AtSample(24, 5_000, 300, -300)
            .AtSample(25, 4_800, 250, -100)
            .AtSample(26, 4_750, 180, 0)
            .AtSample(27, 4_750, 100, 0)
            .AtSample(28, 4_750, 50, 0)
            .Build();
    }

    private static IReadOnlyCollection<FlightEvent> CreateAnchoredEvents(
        IReadOnlyList<FlightTelemetryPoint> telemetry,
        params int[] indexes) =>
        new[]
        {
            new FlightEvent(FlightEventType.Takeoff, telemetry[indexes[0]].Timestamp, telemetry[indexes[0]]),
            indexes.Length > 1 ? new FlightEvent(FlightEventType.TopOfClimb, telemetry[indexes[1]].Timestamp, telemetry[indexes[1]]) : null,
            indexes.Length > 2 ? new FlightEvent(FlightEventType.TopOfDescent, telemetry[indexes[2]].Timestamp, telemetry[indexes[2]]) : null,
            indexes.Length > 3 ? new FlightEvent(FlightEventType.Landing, telemetry[indexes[3]].Timestamp, telemetry[indexes[3]]) : null
        }
        .Where(flightEvent => flightEvent is not null)
        .Cast<FlightEvent>()
        .ToArray();
}
