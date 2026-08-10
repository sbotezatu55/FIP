using Fip.Application.Abstractions.Telemetry;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Telemetry;

/// <summary>
/// Detects interruptions in temporal continuity between normalized telemetry observations.
/// </summary>
public sealed class TelemetryGapDetector : ITelemetryGapDetector
{
    /// <summary>
    /// Initial heuristic for the maximum expected interval between observations.
    /// </summary>
    public static readonly TimeSpan DefaultGapThreshold = TimeSpan.FromSeconds(30);

    public TelemetryGapDetector(TimeSpan? gapThreshold = null)
    {
        GapThreshold = gapThreshold ?? DefaultGapThreshold;

        if (GapThreshold <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(gapThreshold), "The gap threshold must be positive.");
        }
    }

    public TimeSpan GapThreshold { get; }

    public IReadOnlyList<TelemetryGap> Detect(IReadOnlyList<FlightTelemetryPoint> telemetryPoints)
    {
        ArgumentNullException.ThrowIfNull(telemetryPoints);

        if (telemetryPoints.Count < 2)
        {
            return Array.Empty<TelemetryGap>();
        }

        var orderedTelemetryPoints = telemetryPoints
            .OrderBy(point => point.Timestamp)
            .ToList();

        var gaps = new List<TelemetryGap>();

        for (var index = 1; index < orderedTelemetryPoints.Count; index++)
        {
            var previousTimestamp = orderedTelemetryPoints[index - 1].Timestamp;
            var nextTimestamp = orderedTelemetryPoints[index].Timestamp;
            var duration = nextTimestamp - previousTimestamp;

            if (duration > GapThreshold)
            {
                gaps.Add(new TelemetryGap(previousTimestamp, nextTimestamp, duration));
            }
        }

        return gaps.AsReadOnly();
    }
}
