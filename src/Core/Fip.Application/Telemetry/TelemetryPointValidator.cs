using Fip.Application.Abstractions.Telemetry;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Telemetry;

/// <summary>
/// Classifies normalized telemetry points without modifying them.
/// </summary>
public sealed class TelemetryPointValidator : ITelemetryPointValidator
{
    // These broad initial heuristics flag unusual values without declaring them invalid.
    private const double SuspiciousAltitudeFeet = 70_000;
    private const double SuspiciousGroundSpeedKnots = 1_200;
    private const double SuspiciousVerticalRateFeetPerMinute = 15_000;

    public TelemetryValidationResult Validate(FlightTelemetryPoint telemetryPoint)
    {
        ArgumentNullException.ThrowIfNull(telemetryPoint);

        var issues = new List<TelemetryValidationIssue>();

        ValidateLatitude(telemetryPoint.Latitude, issues);
        ValidateLongitude(telemetryPoint.Longitude, issues);
        ValidateTrack(telemetryPoint.TrackDegrees, issues);
        ValidateTimestamp(telemetryPoint.Timestamp, issues);
        ValidateSuspiciousValues(telemetryPoint, issues);

        var status = issues.Any(IsInvalidIssue)
            ? TelemetryValidationStatus.Invalid
            : issues.Count > 0
                ? TelemetryValidationStatus.Suspicious
                : TelemetryValidationStatus.Valid;

        return new TelemetryValidationResult(status, issues);
    }

    private static void ValidateLatitude(double? latitude, ICollection<TelemetryValidationIssue> issues)
    {
        if (latitude.HasValue && (!double.IsFinite(latitude.Value) || latitude.Value is < -90 or > 90))
        {
            issues.Add(TelemetryValidationIssue.LatitudeOutOfRange);
        }
    }

    private static void ValidateLongitude(double? longitude, ICollection<TelemetryValidationIssue> issues)
    {
        if (longitude.HasValue && (!double.IsFinite(longitude.Value) || longitude.Value is < -180 or > 180))
        {
            issues.Add(TelemetryValidationIssue.LongitudeOutOfRange);
        }
    }

    private static void ValidateTrack(double? track, ICollection<TelemetryValidationIssue> issues)
    {
        if (track.HasValue && (!double.IsFinite(track.Value) || track.Value is < 0 or >= 360))
        {
            issues.Add(TelemetryValidationIssue.TrackOutOfRange);
        }
    }

    private static void ValidateTimestamp(
        DateTimeOffset timestamp,
        ICollection<TelemetryValidationIssue> issues)
    {
        if (timestamp == DateTimeOffset.MinValue)
        {
            issues.Add(TelemetryValidationIssue.InvalidTimestamp);
        }
    }

    private static void ValidateSuspiciousValues(
        FlightTelemetryPoint telemetryPoint,
        ICollection<TelemetryValidationIssue> issues)
    {
        if (telemetryPoint.AltitudeFeet > SuspiciousAltitudeFeet)
        {
            issues.Add(TelemetryValidationIssue.AltitudeUnusuallyHigh);
        }

        if (telemetryPoint.GroundSpeedKnots > SuspiciousGroundSpeedKnots)
        {
            issues.Add(TelemetryValidationIssue.GroundSpeedUnusuallyHigh);
        }

        if (Math.Abs(telemetryPoint.VerticalRateFeetPerMinute ?? 0) > SuspiciousVerticalRateFeetPerMinute)
        {
            issues.Add(TelemetryValidationIssue.VerticalRateUnusuallyHigh);
        }
    }

    private static bool IsInvalidIssue(TelemetryValidationIssue issue) => issue switch
    {
        TelemetryValidationIssue.LatitudeOutOfRange => true,
        TelemetryValidationIssue.LongitudeOutOfRange => true,
        TelemetryValidationIssue.TrackOutOfRange => true,
        TelemetryValidationIssue.InvalidTimestamp => true,
        _ => false
    };
}
