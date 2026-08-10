using Fip.Application.Abstractions.Flights;
using Fip.Application.Abstractions.Telemetry;
using Fip.Application.Telemetry;
using Fip.Domain.FlightEvents;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Flights;

/// <summary>
/// Detects a conservative landing transition from normalized telemetry.
/// </summary>
public sealed class LandingDetector : ILandingDetector, IFlightEventDetector
{
    private readonly ITelemetryPointValidator _telemetryPointValidator;
    private readonly LandingDetectionOptions _options;

    public LandingDetector(
        ITelemetryPointValidator? telemetryPointValidator = null,
        LandingDetectionOptions? options = null)
    {
        _telemetryPointValidator = telemetryPointValidator ?? new TelemetryPointValidator();
        _options = options ?? new LandingDetectionOptions();

        ValidateOptions(_options);
    }

    public FlightEvent? Detect(IReadOnlyList<FlightTelemetryPoint> telemetryPoints)
    {
        ArgumentNullException.ThrowIfNull(telemetryPoints);

        var orderedTelemetryPoints = telemetryPoints
            .Where(point => _telemetryPointValidator.Validate(point).Status != TelemetryValidationStatus.Invalid)
            .OrderBy(point => point.Timestamp)
            .ToList();

        var minimumPointsForCandidate = Math.Max(
            _options.MinimumApproachDescentSamples + _options.RolloutSamples,
            _options.MinimumApproachDescentSamples + _options.MinimumGoAroundClimbSamples);

        if (orderedTelemetryPoints.Count < minimumPointsForCandidate)
        {
            return null;
        }

        for (var candidateIndex = _options.MinimumApproachDescentSamples;
             candidateIndex <= orderedTelemetryPoints.Count - _options.RolloutSamples;
             candidateIndex++)
        {
            var approach = orderedTelemetryPoints
                .Skip(candidateIndex - _options.MinimumApproachDescentSamples)
                .Take(_options.MinimumApproachDescentSamples)
                .ToList();
            var rollout = orderedTelemetryPoints
                .Skip(candidateIndex)
                .Take(_options.RolloutSamples)
                .ToList();

            var hasSustainedApproachDescent = HasSustainedApproachDescent(approach);
            var hasLowAltitudeTouchdownApproach = HasLowAltitudeTouchdownApproach(approach, rollout);

            if ((!hasSustainedApproachDescent && !hasLowAltitudeTouchdownApproach) ||
                !HasRolloutEvidence(approach, rollout) ||
                HasGoAroundAfter(orderedTelemetryPoints, candidateIndex))
            {
                continue;
            }

            var candidate = orderedTelemetryPoints[candidateIndex];

            return new FlightEvent(
                FlightEventType.Landing,
                candidate.Timestamp,
                candidate,
                "Landing detected from sustained descent and rollout evidence.");
        }

        return null;
    }

    IReadOnlyCollection<FlightEvent> IFlightEventDetector.Detect(
        IReadOnlyList<FlightTelemetryPoint> telemetryPoints)
    {
        var flightEvent = Detect(telemetryPoints);

        return flightEvent is null
            ? Array.Empty<FlightEvent>()
            : new[] { flightEvent };
    }

    private bool HasSustainedApproachDescent(IReadOnlyList<FlightTelemetryPoint> approach)
    {
        var altitudes = approach
            .Select(point => point.AltitudeFeet)
            .ToList();

        if (altitudes.Any(altitude => !altitude.HasValue))
        {
            return false;
        }

        var descendingSteps = altitudes
            .Zip(altitudes.Skip(1), (previous, next) => previous!.Value - next!.Value)
            .Count(delta => delta > 0);
        var altitudeLoss = altitudes[0]!.Value - altitudes[^1]!.Value;

        var descentRates = approach
            .Where(point => point.VerticalRateFeetPerMinute.HasValue)
            .Select(point => point.VerticalRateFeetPerMinute!.Value)
            .ToList();
        var negativeDescentRates = descentRates
            .Count(rate => rate <= -_options.MinimumDescentRateFeetPerMinute);
        var hasDescentRateEvidence = descentRates.Count == 0 ||
                                     negativeDescentRates >= Math.Min(2, descentRates.Count);

        return descendingSteps >= _options.MinimumDescendingAltitudeSteps &&
               altitudeLoss >= _options.MinimumDescentAltitudeLossFeet &&
               hasDescentRateEvidence;
    }

    private bool HasRolloutEvidence(
        IReadOnlyList<FlightTelemetryPoint> approach,
        IReadOnlyList<FlightTelemetryPoint> rollout)
    {
        var altitudes = rollout
            .Select(point => point.AltitudeFeet)
            .ToList();
        var groundspeeds = rollout
            .Select(point => point.GroundSpeedKnots)
            .ToList();

        if (altitudes.Any(altitude => !altitude.HasValue) ||
            groundspeeds.Any(groundspeed => !groundspeed.HasValue))
        {
            return false;
        }

        var altitudeVariation = altitudes.Max()!.Value - altitudes.Min()!.Value;
        var descendingAltitudeSteps = altitudes
            .Zip(altitudes.Skip(1), (previous, next) => previous!.Value - next!.Value)
            .Count(delta => delta > 0);
        var decreasingSpeedSteps = groundspeeds
            .Zip(groundspeeds.Skip(1), (previous, next) => previous!.Value - next!.Value)
            .Count(delta => delta > 0);
        var approachGroundspeed = approach[^1].GroundSpeedKnots;
        var finalGroundspeed = groundspeeds[^1]!.Value;

        var speedRolloutEvidence = finalGroundspeed <= _options.MaximumRolloutGroundspeedKnots &&
                                   approachGroundspeed.HasValue &&
                                   approachGroundspeed.Value - finalGroundspeed >= _options.MinimumGroundspeedReductionKnots &&
                                   decreasingSpeedSteps >= _options.MinimumDecreasingGroundspeedSteps;
        var lowAltitudeTouchdownEvidence = altitudes[^1]!.Value <= _options.MaximumTouchdownAltitudeFeet &&
                                           descendingAltitudeSteps >= 1 &&
                                           rollout.Any(point => point.VerticalRateFeetPerMinute <= -_options.MinimumDescentRateFeetPerMinute);

        return altitudeVariation <= _options.MaximumRolloutAltitudeVariationFeet &&
               (speedRolloutEvidence || lowAltitudeTouchdownEvidence);
    }

    private bool HasLowAltitudeTouchdownApproach(
        IReadOnlyList<FlightTelemetryPoint> approach,
        IReadOnlyList<FlightTelemetryPoint> rollout)
    {
        var approachAltitude = approach[0].AltitudeFeet;
        var finalAltitude = rollout[^1].AltitudeFeet;

        return approachAltitude.HasValue &&
               finalAltitude.HasValue &&
               finalAltitude.Value <= _options.MaximumTouchdownAltitudeFeet &&
               approachAltitude.Value - finalAltitude.Value >= _options.MinimumTouchdownAltitudeLossFeet;
    }

    private bool HasGoAroundAfter(
        IReadOnlyList<FlightTelemetryPoint> telemetryPoints,
        int candidateIndex)
    {
        for (var startIndex = candidateIndex + 1;
             startIndex <= telemetryPoints.Count - _options.MinimumGoAroundClimbSamples;
             startIndex++)
        {
            var window = telemetryPoints
                .Skip(startIndex)
                .Take(_options.MinimumGoAroundClimbSamples)
                .ToList();
            var altitudes = window
                .Select(point => point.AltitudeFeet)
                .ToList();

            if (altitudes.Any(altitude => !altitude.HasValue))
            {
                continue;
            }

            var positiveSteps = altitudes
                .Zip(altitudes.Skip(1), (previous, next) => next!.Value - previous!.Value)
                .Count(delta => delta > 0);
            var altitudeGain = altitudes[^1]!.Value - altitudes[0]!.Value;

            if (positiveSteps >= _options.MinimumGoAroundClimbSamples - 1 &&
                altitudeGain >= _options.MinimumGoAroundAltitudeGainFeet)
            {
                return true;
            }
        }

        return false;
    }

    private static void ValidateOptions(LandingDetectionOptions options)
    {
        if (options.MinimumApproachDescentSamples < 2 ||
            options.MinimumDescentAltitudeLossFeet <= 0 ||
            options.MinimumDescendingAltitudeSteps < 1 ||
            options.MinimumDescendingAltitudeSteps >= options.MinimumApproachDescentSamples ||
            options.MinimumDescentRateFeetPerMinute < 0 ||
            options.RolloutSamples < 2 ||
            options.MaximumRolloutAltitudeVariationFeet < 0 ||
            options.MaximumRolloutGroundspeedKnots < 0 ||
            options.MinimumGroundspeedReductionKnots < 0 ||
            options.MinimumDecreasingGroundspeedSteps < 1 ||
            options.MaximumTouchdownAltitudeFeet < 0 ||
            options.MinimumTouchdownAltitudeLossFeet <= 0 ||
            options.MinimumGoAroundClimbSamples < 2 ||
            options.MinimumGoAroundAltitudeGainFeet <= 0)
        {
            throw new ArgumentException("Landing detection options must define positive, compatible thresholds.", nameof(options));
        }
    }
}
