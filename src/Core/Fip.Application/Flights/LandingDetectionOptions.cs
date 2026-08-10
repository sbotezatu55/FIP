namespace Fip.Application.Flights;

/// <summary>
/// Initial heuristic settings for landing detection.
/// </summary>
public sealed record LandingDetectionOptions
{
    public int MinimumApproachDescentSamples { get; init; } = 4;

    public double MinimumDescentAltitudeLossFeet { get; init; } = 500;

    public int MinimumDescendingAltitudeSteps { get; init; } = 2;

    public double MinimumDescentRateFeetPerMinute { get; init; } = 200;

    public int RolloutSamples { get; init; } = 4;

    public double MaximumRolloutAltitudeVariationFeet { get; init; } = 250;

    public double MaximumRolloutGroundspeedKnots { get; init; } = 100;

    public double MinimumGroundspeedReductionKnots { get; init; } = 20;

    /// <summary>
    /// OpenSky can represent touchdown as one abrupt sampled speed drop.
    /// </summary>
    public int MinimumDecreasingGroundspeedSteps { get; init; } = 1;

    /// <summary>
    /// Permits touchdown evidence when the source holds landing speed through the
    /// rollout but reports a sustained descent into a low-altitude stabilized window.
    /// </summary>
    public double MaximumTouchdownAltitudeFeet { get; init; } = 300;

    public double MinimumTouchdownAltitudeLossFeet { get; init; } = 50;

    public int MinimumGoAroundClimbSamples { get; init; } = 3;

    public double MinimumGoAroundAltitudeGainFeet { get; init; } = 300;
}
