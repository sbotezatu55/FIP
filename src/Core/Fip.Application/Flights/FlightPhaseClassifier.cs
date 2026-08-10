using Fip.Application.Abstractions.Flights;
using Fip.Application.Abstractions.Telemetry;
using Fip.Application.Telemetry;
using Fip.Domain.FlightEvents;
using Fip.Domain.Flights.Phases;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Flights;

/// <summary>
/// Classifies normalized telemetry into conservative, contiguous operational phase segments.
/// </summary>
public sealed class FlightPhaseClassifier : IFlightPhaseClassifier
{
    private readonly ITelemetryPointValidator _telemetryPointValidator;
    private readonly FlightPhaseClassificationOptions _options;

    public FlightPhaseClassifier(
        ITelemetryPointValidator? telemetryPointValidator = null,
        FlightPhaseClassificationOptions? options = null)
    {
        _telemetryPointValidator = telemetryPointValidator ?? new TelemetryPointValidator();
        _options = options ?? new FlightPhaseClassificationOptions();

        ValidateOptions(_options);
    }

    public IReadOnlyCollection<FlightPhaseSegment> Classify(
        IReadOnlyList<FlightTelemetryPoint> telemetryPoints,
        IReadOnlyCollection<FlightEvent> events)
    {
        ArgumentNullException.ThrowIfNull(telemetryPoints);
        ArgumentNullException.ThrowIfNull(events);

        var orderedPoints = telemetryPoints
            .Where(point => _telemetryPointValidator.Validate(point).Status != TelemetryValidationStatus.Invalid)
            .OrderBy(point => point.Timestamp)
            .ToList();

        if (orderedPoints.Count == 0)
        {
            return Array.Empty<FlightPhaseSegment>();
        }

        var boundaries = FindEventBoundaries(orderedPoints, events);
        var phases = orderedPoints
            .Select((point, index) => DeterminePhase(orderedPoints, index, boundaries))
            .ToList();

        return BuildSegments(orderedPoints, phases);
    }

    private FlightPhase DeterminePhase(
        IReadOnlyList<FlightTelemetryPoint> points,
        int index,
        EventBoundaries boundaries)
    {
        if (index > 0 && points[index].Timestamp - points[index - 1].Timestamp > _options.MaximumContinuousTelemetryGap)
        {
            return FlightPhase.Unknown;
        }

        var point = points[index];

        if (boundaries.LandingIndex is int landingIndex && index >= landingIndex)
        {
            return FlightPhase.LandingRoll;
        }

        if (boundaries.TakeoffIndex is int takeoffIndex && index < takeoffIndex)
        {
            return IsTakeoffRoll(points, index, takeoffIndex)
                ? FlightPhase.TakeoffRoll
                : FlightPhase.Ground;
        }

        if (boundaries.TakeoffIndex is int detectedTakeoffIndex && index >= detectedTakeoffIndex)
        {
            if (boundaries.TopOfClimbIndex is not int topOfClimbIndex || index < topOfClimbIndex)
            {
                return IsWithinInitialClimb(points[detectedTakeoffIndex], point)
                    ? FlightPhase.InitialClimb
                    : FlightPhase.Climb;
            }
        }

        if (boundaries.TopOfDescentIndex is int topOfDescentIndex && index >= topOfDescentIndex)
        {
            if (boundaries.LandingIndex is int detectedLandingIndex &&
                points[detectedLandingIndex].Timestamp - point.Timestamp <= _options.ApproachWindow)
            {
                return FlightPhase.Approach;
            }

            return FlightPhase.Descent;
        }

        if (boundaries.TopOfClimbIndex is not null &&
            (boundaries.TopOfDescentIndex is null || index < boundaries.TopOfDescentIndex))
        {
            return FlightPhase.Cruise;
        }

        return DetermineFromLocalTelemetry(points, index);
    }

    private bool IsTakeoffRoll(
        IReadOnlyList<FlightTelemetryPoint> points,
        int index,
        int takeoffIndex)
    {
        if (index < Math.Max(1, takeoffIndex - 2) ||
            points[index].GroundSpeedKnots is not double speed ||
            speed < _options.TakeoffRollMinimumGroundSpeedKnots)
        {
            return false;
        }

        var previousSpeed = points[index - 1].GroundSpeedKnots;
        var altitude = points[index].AltitudeFeet;
        var previousAltitude = points[index - 1].AltitudeFeet;

        return previousSpeed.HasValue && speed >= previousSpeed.Value &&
               altitude.HasValue && previousAltitude.HasValue &&
               Math.Abs(altitude.Value - previousAltitude.Value) <= _options.InitialClimbAltitudeGainFeet / 3;
    }

    private bool IsWithinInitialClimb(
        FlightTelemetryPoint takeoffPoint,
        FlightTelemetryPoint point)
    {
        var elapsed = point.Timestamp - takeoffPoint.Timestamp;
        var altitudeGain = point.AltitudeFeet.HasValue && takeoffPoint.AltitudeFeet.HasValue
            ? point.AltitudeFeet.Value - takeoffPoint.AltitudeFeet.Value
            : 0;

        return elapsed <= _options.InitialClimbDuration ||
               altitudeGain < _options.InitialClimbAltitudeGainFeet;
    }

    private FlightPhase DetermineFromLocalTelemetry(IReadOnlyList<FlightTelemetryPoint> points, int index)
    {
        if (IsGroundLike(points[index]))
        {
            return FlightPhase.Ground;
        }

        if (IsClimbing(points, index))
        {
            return FlightPhase.Climb;
        }

        if (IsDescending(points, index))
        {
            return FlightPhase.Descent;
        }

        return IsLevelLike(points, index) ? FlightPhase.Cruise : FlightPhase.Unknown;
    }

    private bool IsGroundLike(FlightTelemetryPoint point) =>
        point.GroundSpeedKnots is double speed && speed <= _options.GroundGroundSpeedThresholdKnots &&
        Math.Abs(point.VerticalRateFeetPerMinute ?? 0) <= _options.GroundVerticalRateToleranceFeetPerMinute;

    private bool IsClimbing(IReadOnlyList<FlightTelemetryPoint> points, int index)
    {
        var previous = index > 0 ? points[index - 1] : null;
        var next = index + 1 < points.Count ? points[index + 1] : null;
        var verticalRate = points[index].VerticalRateFeetPerMinute;
        var altitudeTrend = GetAltitudeDifference(previous, points[index]) + GetAltitudeDifference(points[index], next);

        return verticalRate >= _options.MinimumClimbVerticalRateFeetPerMinute ||
               altitudeTrend >= _options.MinimumAltitudeTrendFeet;
    }

    private bool IsDescending(IReadOnlyList<FlightTelemetryPoint> points, int index)
    {
        var previous = index > 0 ? points[index - 1] : null;
        var next = index + 1 < points.Count ? points[index + 1] : null;
        var verticalRate = points[index].VerticalRateFeetPerMinute;
        var altitudeTrend = GetAltitudeDifference(previous, points[index]) + GetAltitudeDifference(points[index], next);

        return verticalRate <= -_options.MinimumDescentVerticalRateFeetPerMinute ||
               altitudeTrend <= -_options.MinimumAltitudeTrendFeet;
    }

    private bool IsLevelLike(IReadOnlyList<FlightTelemetryPoint> points, int index)
    {
        var start = Math.Max(0, index - 2);
        var end = Math.Min(points.Count - 1, index + 2);
        var window = points.Skip(start).Take(end - start + 1).ToList();
        var altitudes = window.Where(point => point.AltitudeFeet.HasValue).Select(point => point.AltitudeFeet!.Value).ToList();

        return altitudes.Count == window.Count &&
               altitudes.Max() - altitudes.Min() <= _options.CruiseAltitudeVariationFeet &&
               window.All(point => Math.Abs(point.VerticalRateFeetPerMinute ?? 0) <= _options.LevelVerticalRateToleranceFeetPerMinute);
    }

    private static double GetAltitudeDifference(FlightTelemetryPoint? first, FlightTelemetryPoint? second) =>
        first?.AltitudeFeet is double firstAltitude && second?.AltitudeFeet is double secondAltitude
            ? secondAltitude - firstAltitude
            : 0;

    private IReadOnlyCollection<FlightPhaseSegment> BuildSegments(
        IReadOnlyList<FlightTelemetryPoint> points,
        IReadOnlyList<FlightPhase> phases)
    {
        var segments = new List<FlightPhaseSegment>();
        var segmentStart = 0;

        for (var index = 1; index <= points.Count; index++)
        {
            if (index < points.Count && phases[index] == phases[segmentStart])
            {
                continue;
            }

            segments.Add(new FlightPhaseSegment(
                phases[segmentStart],
                points[segmentStart].Timestamp,
                points[index - 1].Timestamp,
                points[segmentStart],
                points[index - 1]));

            segmentStart = index;
        }

        return segments.AsReadOnly();
    }

    private static EventBoundaries FindEventBoundaries(
        IReadOnlyList<FlightTelemetryPoint> points,
        IReadOnlyCollection<FlightEvent> events)
    {
        return new EventBoundaries(
            FindEventIndex(points, events, FlightEventType.Takeoff),
            FindEventIndex(points, events, FlightEventType.TopOfClimb),
            FindEventIndex(points, events, FlightEventType.TopOfDescent),
            FindEventIndex(points, events, FlightEventType.Landing));
    }

    private static int? FindEventIndex(
        IReadOnlyList<FlightTelemetryPoint> points,
        IReadOnlyCollection<FlightEvent> events,
        FlightEventType type)
    {
        var flightEvent = events
            .Where(item => item.Type == type)
            .OrderBy(item => item.Timestamp)
            .FirstOrDefault();

        if (flightEvent is null)
        {
            return null;
        }

        return Enumerable.Range(0, points.Count)
            .OrderBy(index => Math.Abs((points[index].Timestamp - flightEvent.Timestamp).Ticks))
            .First();
    }

    private static void ValidateOptions(FlightPhaseClassificationOptions options)
    {
        if (options.GroundGroundSpeedThresholdKnots < 0 ||
            options.GroundVerticalRateToleranceFeetPerMinute < 0 ||
            options.TakeoffRollMinimumGroundSpeedKnots < 0 ||
            options.InitialClimbDuration <= TimeSpan.Zero ||
            options.InitialClimbAltitudeGainFeet < 0 ||
            options.MinimumClimbVerticalRateFeetPerMinute < 0 ||
            options.MinimumDescentVerticalRateFeetPerMinute < 0 ||
            options.MinimumAltitudeTrendFeet < 0 ||
            options.LevelVerticalRateToleranceFeetPerMinute < 0 ||
            options.CruiseAltitudeVariationFeet < 0 ||
            options.ApproachWindow <= TimeSpan.Zero ||
            options.MaximumContinuousTelemetryGap <= TimeSpan.Zero)
        {
            throw new ArgumentException("Flight-phase classification options must be non-negative with positive durations.", nameof(options));
        }
    }

    private sealed record EventBoundaries(
        int? TakeoffIndex,
        int? TopOfClimbIndex,
        int? TopOfDescentIndex,
        int? LandingIndex);
}
