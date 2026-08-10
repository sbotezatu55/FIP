using Fip.Application.Abstractions.Flights;
using Fip.Application.Flights;
using Fip.Application.Telemetry;
using Fip.Domain.FlightEvents;
using Fip.Domain.Flights.Phases;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Tests;

public sealed class FlightEventTimelineTests
{
    [Fact]
    public void Detect_ReturnsChronologicalFullFlightEventTimeline()
    {
        var telemetry = new SyntheticTelemetryBuilder()
            .WithSamplingInterval(TimeSpan.FromSeconds(30))
            .AtSample(0, 1_000, 10, 0)
            .AtSample(1, 1_010, 30, 50)
            .AtSample(2, 1_020, 70, 150)
            .AtSample(3, 1_200, 100, 800)
            .AtSample(4, 1_500, 120, 1_000)
            .AtSample(5, 1_900, 140, 1_000)
            .AtSample(6, 2_300, 160, 1_000)
            .AtSample(7, 5_000, 180, 1_000)
            .AtSample(8, 10_000, 200, 1_000)
            .AtSample(9, 16_000, 220, 1_000)
            .AtSample(10, 22_000, 240, 1_000)
            .AtSample(11, 28_000, 250, 1_000)
            .AtSample(12, 30_000, 450, 0)
            .AtSample(13, 30_050, 450, 0)
            .AtSample(14, 30_000, 450, 0)
            .AtSample(15, 30_025, 450, 0)
            .AtSample(16, 30_000, 450, 0)
            .AtSample(17, 29_500, 440, -300)
            .AtSample(18, 28_500, 430, -500)
            .AtSample(19, 27_500, 420, -500)
            .AtSample(20, 26_500, 410, -500)
            .AtSample(21, 25_000, 400, -500)
            .AtSample(22, 24_000, 380, -600)
            .AtSample(23, 18_000, 300, -700)
            .AtSample(24, 12_000, 220, -700)
            .AtSample(25, 6_000, 160, -600)
            .AtSample(26, 5_900, 140, -100)
            .AtSample(27, 5_850, 120, 0)
            .AtSample(28, 5_870, 90, 0)
            .AtSample(29, 5_850, 60, 0)
            .Build();

        var service = new FlightEventDetectionService(new IFlightEventDetector[]
        {
            new TakeoffDetector(new TelemetryPointValidator()),
            new TopOfClimbDetector(new TelemetryPointValidator()),
            new TopOfDescentDetector(new TelemetryPointValidator()),
            new LandingDetector(new TelemetryPointValidator())
        });

        var events = service.Detect(telemetry);

        Assert.Equal(
            new[]
            {
                FlightEventType.Takeoff,
                FlightEventType.TopOfClimb,
                FlightEventType.TopOfDescent,
                FlightEventType.Landing
            },
            events.Select(flightEvent => flightEvent.Type));
        Assert.True(events.Zip(events.Skip(1), (first, second) => first.Timestamp <= second.Timestamp).All(result => result));

        var phases = new FlightPhaseClassifier(new TelemetryPointValidator()).Classify(telemetry, events).ToList();

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
            phases.Select(segment => segment.Phase));

        var takeoff = events.Single(flightEvent => flightEvent.Type == FlightEventType.Takeoff);
        var topOfClimb = events.Single(flightEvent => flightEvent.Type == FlightEventType.TopOfClimb);
        var topOfDescent = events.Single(flightEvent => flightEvent.Type == FlightEventType.TopOfDescent);
        var landing = events.Single(flightEvent => flightEvent.Type == FlightEventType.Landing);

        Assert.Contains(phases, segment => segment.Phase == FlightPhase.InitialClimb &&
                                           segment.StartTimestamp <= takeoff.Timestamp &&
                                           segment.EndTimestamp >= takeoff.Timestamp);
        Assert.Contains(phases, segment => segment.Phase == FlightPhase.Cruise &&
                                           segment.StartTimestamp <= topOfClimb.Timestamp &&
                                           segment.EndTimestamp >= topOfClimb.Timestamp);
        Assert.Contains(phases, segment => segment.Phase == FlightPhase.Descent &&
                                           segment.StartTimestamp <= topOfDescent.Timestamp);
        Assert.Contains(phases, segment => segment.Phase == FlightPhase.LandingRoll &&
                                           segment.StartTimestamp <= landing.Timestamp);
    }
}
