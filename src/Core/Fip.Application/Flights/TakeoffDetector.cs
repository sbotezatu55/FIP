using Fip.Application.Abstractions.Flights;
using Fip.Application.Abstractions.Telemetry;
using Fip.Application.Telemetry;
using Fip.Domain.FlightEvents;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Flights;

/// <summary>
/// Detects a conservative takeoff transition from normalized telemetry.
/// </summary>
public sealed class TakeoffDetector : ITakeoffDetector, IFlightEventDetector
{
    private readonly ITelemetryPointValidator _telemetryPointValidator;
    private readonly TakeoffDetectionOptions _options;

    public TakeoffDetector(
        ITelemetryPointValidator? telemetryPointValidator = null,
        TakeoffDetectionOptions? options = null)
    {
        _telemetryPointValidator = telemetryPointValidator ?? new TelemetryPointValidator();
        _options = options ?? new TakeoffDetectionOptions();

        ValidateOptions(_options);
    }

    public FlightEvent? Detect(IReadOnlyList<FlightTelemetryPoint> telemetryPoints)
    {
        ArgumentNullException.ThrowIfNull(telemetryPoints);

        var orderedTelemetryPoints = telemetryPoints
            .Where(point => _telemetryPointValidator.Validate(point).Status != TelemetryValidationStatus.Invalid)
            .OrderBy(point => point.Timestamp)
            .ToList();

        var minimumCandidateIndex = _options.MinimumPreTakeoffSamples;
        var minimumPointsForCandidate = Math.Max(
            _options.SustainedClimbSamples,
            _options.MinimumAirborneSamplesAfterCandidate + 1);

        if (orderedTelemetryPoints.Count < minimumCandidateIndex + minimumPointsForCandidate)
        {
            return null;
        }

        for (var candidateIndex = minimumCandidateIndex;
             candidateIndex <= orderedTelemetryPoints.Count - minimumPointsForCandidate;
             candidateIndex++)
        {
            var candidate = orderedTelemetryPoints[candidateIndex];

            if (!HasGroundspeedTransition(orderedTelemetryPoints, candidateIndex))
            {
                continue;
            }

            var climbWindow = orderedTelemetryPoints
                .Skip(candidateIndex)
                .Take(_options.SustainedClimbSamples)
                .ToList();

            if (!HasSustainedClimb(climbWindow) ||
                !RemainsAirborne(orderedTelemetryPoints, candidateIndex))
            {
                continue;
            }

            return new FlightEvent(
                FlightEventType.Takeoff,
                candidate.Timestamp,
                candidate,
                "Takeoff detected from sustained groundspeed and climb evidence.");
        }

        var initialClimbWindow = orderedTelemetryPoints
            .Take(_options.InitialClimbSamples)
            .ToList();

        if (HasInitialClimbEvidence(initialClimbWindow))
        {
            var candidate = initialClimbWindow[0];

            return new FlightEvent(
                FlightEventType.Takeoff,
                candidate.Timestamp,
                candidate,
                "Takeoff inferred from low-altitude sustained climb at the start of the trajectory.");
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

    private bool HasGroundspeedTransition(
        IReadOnlyList<FlightTelemetryPoint> telemetryPoints,
        int candidateIndex)
    {
        var candidateGroundspeed = telemetryPoints[candidateIndex].GroundSpeedKnots;

        if (!candidateGroundspeed.HasValue ||
            candidateGroundspeed.Value < _options.MinimumTakeoffGroundspeedKnots)
        {
            return false;
        }

        var precedingPoints = telemetryPoints
            .Skip(candidateIndex - _options.MinimumPreTakeoffSamples)
            .Take(_options.MinimumPreTakeoffSamples);

        return precedingPoints.Any(point =>
            point.GroundSpeedKnots.HasValue &&
            point.GroundSpeedKnots.Value < _options.MinimumTakeoffGroundspeedKnots);
    }

    private bool HasSustainedClimb(IReadOnlyList<FlightTelemetryPoint> climbWindow)
    {
        var altitudes = climbWindow
            .Select(point => point.AltitudeFeet)
            .ToList();

        if (altitudes.Any(altitude => !altitude.HasValue))
        {
            return false;
        }

        var positiveAltitudeSteps = altitudes
            .Zip(altitudes.Skip(1), (previous, next) => next!.Value - previous!.Value)
            .Count(delta => delta > 0);

        var altitudeGain = altitudes[^1]!.Value - altitudes[0]!.Value;
        var climbRates = climbWindow
            .Where(point => point.VerticalRateFeetPerMinute.HasValue)
            .Select(point => point.VerticalRateFeetPerMinute!.Value)
            .ToList();

        var positiveClimbRates = climbRates.Count(rate => rate >= _options.MinimumClimbRateFeetPerMinute);
        var hasClimbRateEvidence = climbRates.Count == 0 || positiveClimbRates >= 2;

        return positiveAltitudeSteps >= _options.MinimumPositiveAltitudeSteps &&
               altitudeGain >= _options.MinimumAltitudeGainFeet &&
               hasClimbRateEvidence;
    }

    private bool RemainsAirborne(
        IReadOnlyList<FlightTelemetryPoint> telemetryPoints,
        int candidateIndex)
    {
        var followingPoints = telemetryPoints
            .Skip(candidateIndex + 1)
            .Take(_options.MinimumAirborneSamplesAfterCandidate)
            .ToList();

        if (followingPoints.Count < _options.MinimumAirborneSamplesAfterCandidate)
        {
            return false;
        }

        var candidateAltitude = telemetryPoints[candidateIndex].AltitudeFeet;
        var finalAltitude = followingPoints[^1].AltitudeFeet;

        return candidateAltitude.HasValue &&
               finalAltitude.HasValue &&
               finalAltitude.Value - candidateAltitude.Value >= _options.MinimumAltitudeGainFeet;
    }

    private bool HasInitialClimbEvidence(IReadOnlyList<FlightTelemetryPoint> initialClimbWindow)
    {
        var initialAltitude = initialClimbWindow[0].AltitudeFeet;
        var initialGroundspeed = initialClimbWindow[0].GroundSpeedKnots;

        if (initialClimbWindow.Count < _options.InitialClimbSamples ||
            initialAltitude is null ||
            initialAltitude > _options.MaximumInitialAltitudeFeet ||
            initialGroundspeed is null ||
            initialGroundspeed.Value < _options.MinimumTakeoffGroundspeedKnots)
        {
            return false;
        }

        return HasSustainedClimb(initialClimbWindow);
    }

    private static void ValidateOptions(TakeoffDetectionOptions options)
    {
        if (options.MinimumTakeoffGroundspeedKnots < 0 ||
            options.MinimumAltitudeGainFeet <= 0 ||
            options.MinimumPreTakeoffSamples < 1 ||
            options.SustainedClimbSamples < 2 ||
            options.MinimumPositiveAltitudeSteps < 1 ||
            options.MinimumPositiveAltitudeSteps >= options.SustainedClimbSamples ||
            options.MinimumAirborneSamplesAfterCandidate < 1 ||
            options.MinimumClimbRateFeetPerMinute < 0 ||
            options.InitialClimbSamples < options.SustainedClimbSamples ||
            options.MaximumInitialAltitudeFeet <= 0)
        {
            throw new ArgumentException("Takeoff detection options must define positive, compatible thresholds.", nameof(options));
        }
    }
}
