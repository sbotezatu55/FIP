using Fip.Application.Abstractions.Flights;
using Fip.Application.Abstractions.Telemetry;
using Fip.Application.Telemetry;
using Fip.Domain.FlightEvents;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Flights;

/// <summary>
/// Detects the transition from an established cruise segment into sustained descent.
/// </summary>
public sealed class TopOfDescentDetector : ITopOfDescentDetector, IFlightEventDetector
{
    private readonly ITelemetryPointValidator _telemetryPointValidator;
    private readonly TopOfDescentDetectionOptions _options;

    public TopOfDescentDetector(
        ITelemetryPointValidator? telemetryPointValidator = null,
        TopOfDescentDetectionOptions? options = null)
    {
        _telemetryPointValidator = telemetryPointValidator ?? new TelemetryPointValidator();
        _options = options ?? new TopOfDescentDetectionOptions();

        ValidateOptions(_options);
    }

    public FlightEvent? Detect(IReadOnlyList<FlightTelemetryPoint> telemetryPoints)
    {
        ArgumentNullException.ThrowIfNull(telemetryPoints);

        var orderedTelemetryPoints = telemetryPoints
            .Where(point => _telemetryPointValidator.Validate(point).Status != TelemetryValidationStatus.Invalid)
            .OrderBy(point => point.Timestamp)
            .ToList();

        var minimumPointsForCandidate = _options.MinimumCruiseSamples + _options.DescentConfirmationSamples;

        if (orderedTelemetryPoints.Count < minimumPointsForCandidate)
        {
            return null;
        }

        for (var candidateIndex = _options.MinimumCruiseSamples;
             candidateIndex <= orderedTelemetryPoints.Count - _options.DescentConfirmationSamples;
             candidateIndex++)
        {
            var cruiseWindow = orderedTelemetryPoints
                .Skip(candidateIndex - _options.MinimumCruiseSamples)
                .Take(_options.MinimumCruiseSamples)
                .ToList();
            var descentWindow = orderedTelemetryPoints
                .Skip(candidateIndex)
                .Take(_options.DescentConfirmationSamples)
                .ToList();

            if (!IsEstablishedCruise(cruiseWindow) ||
                !HasAcceptableContinuity(cruiseWindow, descentWindow) ||
                !IsEstablishedDescent(descentWindow) ||
                HasAbortedDescent(orderedTelemetryPoints, candidateIndex))
            {
                continue;
            }

            var candidate = orderedTelemetryPoints[candidateIndex];

            return new FlightEvent(
                FlightEventType.TopOfDescent,
                candidate.Timestamp,
                candidate,
                "Top of descent detected after established cruise and sustained descent evidence.");
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

    private bool IsEstablishedCruise(IReadOnlyList<FlightTelemetryPoint> cruiseWindow)
    {
        var altitudes = cruiseWindow
            .Select(point => point.AltitudeFeet)
            .ToList();

        if (altitudes.Any(altitude => !altitude.HasValue))
        {
            return false;
        }

        var altitudeVariation = altitudes.Max()!.Value - altitudes.Min()!.Value;
        var verticalRates = cruiseWindow
            .Where(point => point.VerticalRateFeetPerMinute.HasValue)
            .Select(point => Math.Abs(point.VerticalRateFeetPerMinute!.Value))
            .ToList();

        return altitudeVariation <= _options.MaximumCruiseAltitudeVariationFeet &&
               verticalRates.All(rate => rate <= _options.CruiseVerticalRateToleranceFeetPerMinute);
    }

    private bool IsEstablishedDescent(IReadOnlyList<FlightTelemetryPoint> descentWindow)
    {
        var altitudes = descentWindow
            .Select(point => point.AltitudeFeet)
            .ToList();

        if (altitudes.Any(altitude => !altitude.HasValue) ||
            descentWindow[^1].Timestamp - descentWindow[0].Timestamp < _options.MinimumDescentDuration)
        {
            return false;
        }

        var descendingSteps = altitudes
            .Zip(altitudes.Skip(1), (previous, next) => previous!.Value - next!.Value)
            .Count(delta => delta > 0);
        var altitudeLoss = altitudes[0]!.Value - altitudes[^1]!.Value;
        var negativeVerticalRates = descentWindow
            .Where(point => point.VerticalRateFeetPerMinute.HasValue)
            .Count(point => point.VerticalRateFeetPerMinute!.Value <=
                            -_options.MinimumDescentVerticalRateFeetPerMinute);

        var verticalRateSamples = descentWindow.Count(point => point.VerticalRateFeetPerMinute.HasValue);
        var hasVerticalRateEvidence = verticalRateSamples == 0 ||
                                      negativeVerticalRates >= Math.Min(
                                          _options.MinimumNegativeVerticalRateSamples,
                                          verticalRateSamples);

        return descendingSteps >= _options.DescentConfirmationSamples - 2 &&
               altitudeLoss >= _options.MinimumDescentAltitudeLossFeet &&
               hasVerticalRateEvidence;
    }

    private bool HasAcceptableContinuity(
        IReadOnlyList<FlightTelemetryPoint> cruiseWindow,
        IReadOnlyList<FlightTelemetryPoint> descentWindow)
    {
        var points = cruiseWindow.Concat(descentWindow).ToList();

        return points
            .Zip(points.Skip(1), (previous, next) => next.Timestamp - previous.Timestamp)
            .All(interval => interval <= _options.MaximumConfirmationGap);
    }

    private bool HasAbortedDescent(
        IReadOnlyList<FlightTelemetryPoint> telemetryPoints,
        int candidateIndex)
    {
        for (var startIndex = candidateIndex + _options.DescentConfirmationSamples;
             startIndex <= telemetryPoints.Count - _options.RecoveryConfirmationSamples;
             startIndex++)
        {
            var recoveryWindow = telemetryPoints
                .Skip(startIndex)
                .Take(_options.RecoveryConfirmationSamples)
                .ToList();
            var altitudes = recoveryWindow
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

            if (positiveSteps >= _options.RecoveryConfirmationSamples - 1 &&
                altitudeGain >= _options.MinimumRecoveryAltitudeGainFeet)
            {
                return true;
            }
        }

        return false;
    }

    private static void ValidateOptions(TopOfDescentDetectionOptions options)
    {
        if (options.MinimumCruiseSamples < 2 ||
            options.MaximumCruiseAltitudeVariationFeet < 0 ||
            options.CruiseVerticalRateToleranceFeetPerMinute < 0 ||
            options.DescentConfirmationSamples < 3 ||
            options.MinimumDescentDuration <= TimeSpan.Zero ||
            options.MinimumDescentAltitudeLossFeet <= 0 ||
            options.MinimumNegativeVerticalRateSamples < 1 ||
            options.MinimumDescentVerticalRateFeetPerMinute < 0 ||
            options.MaximumConfirmationGap <= TimeSpan.Zero ||
            options.RecoveryConfirmationSamples < 2 ||
            options.MinimumRecoveryAltitudeGainFeet <= 0)
        {
            throw new ArgumentException("Top-of-descent options must define positive, compatible thresholds.", nameof(options));
        }
    }
}
