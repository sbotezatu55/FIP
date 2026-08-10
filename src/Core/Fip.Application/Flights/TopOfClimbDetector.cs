using Fip.Application.Abstractions.Flights;
using Fip.Application.Abstractions.Telemetry;
using Fip.Application.Telemetry;
using Fip.Domain.FlightEvents;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Flights;

/// <summary>
/// Detects the first established transition from climb into sustained level flight.
/// </summary>
public sealed class TopOfClimbDetector : ITopOfClimbDetector, IFlightEventDetector
{
    private readonly ITelemetryPointValidator _telemetryPointValidator;
    private readonly TopOfClimbDetectionOptions _options;

    public TopOfClimbDetector(
        ITelemetryPointValidator? telemetryPointValidator = null,
        TopOfClimbDetectionOptions? options = null)
    {
        _telemetryPointValidator = telemetryPointValidator ?? new TelemetryPointValidator();
        _options = options ?? new TopOfClimbDetectionOptions();

        ValidateOptions(_options);
    }

    public FlightEvent? Detect(IReadOnlyList<FlightTelemetryPoint> telemetryPoints)
    {
        ArgumentNullException.ThrowIfNull(telemetryPoints);

        var orderedTelemetryPoints = telemetryPoints
            .Where(point => _telemetryPointValidator.Validate(point).Status != TelemetryValidationStatus.Invalid)
            .OrderBy(point => point.Timestamp)
            .ToList();

        var minimumPointsForCandidate = _options.MinimumClimbSamples + _options.LevelConfirmationSamples;

        if (orderedTelemetryPoints.Count < minimumPointsForCandidate)
        {
            return null;
        }

        for (var candidateIndex = _options.MinimumClimbSamples;
             candidateIndex <= orderedTelemetryPoints.Count - _options.LevelConfirmationSamples;
             candidateIndex++)
        {
            var climbWindow = orderedTelemetryPoints
                .Skip(candidateIndex - _options.MinimumClimbSamples)
                .Take(_options.MinimumClimbSamples)
                .ToList();
            var levelWindow = orderedTelemetryPoints
                .Skip(candidateIndex)
                .Take(_options.LevelConfirmationSamples)
                .ToList();

            if (!IsEstablishedClimb(climbWindow) ||
                !HasAcceptableContinuity(climbWindow, levelWindow) ||
                !IsEstablishedLevel(levelWindow))
            {
                continue;
            }

            var candidate = orderedTelemetryPoints[candidateIndex];

            return new FlightEvent(
                FlightEventType.TopOfClimb,
                candidate.Timestamp,
                candidate,
                "Top of climb detected after sustained climb and level-flight evidence.");
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

    private bool IsEstablishedClimb(IReadOnlyList<FlightTelemetryPoint> climbWindow)
    {
        var altitudes = climbWindow
            .Select(point => point.AltitudeFeet)
            .ToList();

        if (altitudes.Any(altitude => !altitude.HasValue) ||
            climbWindow[^1].Timestamp - climbWindow[0].Timestamp < _options.MinimumClimbDuration)
        {
            return false;
        }

        var ascendingSteps = altitudes
            .Zip(altitudes.Skip(1), (previous, next) => next!.Value - previous!.Value)
            .Count(delta => delta > 0);
        var altitudeGain = altitudes[^1]!.Value - altitudes[0]!.Value;
        var positiveVerticalRates = climbWindow
            .Where(point => point.VerticalRateFeetPerMinute.HasValue)
            .Count(point => point.VerticalRateFeetPerMinute!.Value >=
                            _options.MinimumClimbVerticalRateFeetPerMinute);
        var verticalRateSamples = climbWindow.Count(point => point.VerticalRateFeetPerMinute.HasValue);
        var hasVerticalRateEvidence = verticalRateSamples == 0 ||
                                      positiveVerticalRates >= Math.Min(2, verticalRateSamples);

        return ascendingSteps >= _options.MinimumClimbSamples - 2 &&
               altitudeGain >= _options.MinimumClimbAltitudeGainFeet &&
               hasVerticalRateEvidence;
    }

    private bool IsEstablishedLevel(IReadOnlyList<FlightTelemetryPoint> levelWindow)
    {
        var altitudes = levelWindow
            .Select(point => point.AltitudeFeet)
            .ToList();

        if (altitudes.Any(altitude => !altitude.HasValue) ||
            levelWindow[^1].Timestamp - levelWindow[0].Timestamp < _options.MinimumLevelDuration)
        {
            return false;
        }

        var altitudeVariation = altitudes.Max()!.Value - altitudes.Min()!.Value;
        var verticalRates = levelWindow
            .Where(point => point.VerticalRateFeetPerMinute.HasValue)
            .Select(point => Math.Abs(point.VerticalRateFeetPerMinute!.Value))
            .ToList();

        return altitudeVariation <= _options.MaximumLevelAltitudeVariationFeet &&
               verticalRates.All(rate => rate <= _options.LevelVerticalRateToleranceFeetPerMinute);
    }

    private bool HasAcceptableContinuity(
        IReadOnlyList<FlightTelemetryPoint> climbWindow,
        IReadOnlyList<FlightTelemetryPoint> levelWindow)
    {
        var points = climbWindow.Concat(levelWindow).ToList();

        return points
            .Zip(points.Skip(1), (previous, next) => next.Timestamp - previous.Timestamp)
            .All(interval => interval <= _options.MaximumConfirmationGap);
    }

    private static void ValidateOptions(TopOfClimbDetectionOptions options)
    {
        if (options.MinimumClimbSamples < 2 ||
            options.MinimumClimbDuration <= TimeSpan.Zero ||
            options.MinimumClimbAltitudeGainFeet <= 0 ||
            options.MinimumClimbVerticalRateFeetPerMinute < 0 ||
            options.LevelConfirmationSamples < 3 ||
            options.MinimumLevelDuration <= TimeSpan.Zero ||
            options.MaximumLevelAltitudeVariationFeet < 0 ||
            options.LevelVerticalRateToleranceFeetPerMinute < 0 ||
            options.MaximumConfirmationGap <= TimeSpan.Zero)
        {
            throw new ArgumentException("Top-of-climb options must define positive, compatible thresholds.", nameof(options));
        }
    }
}
