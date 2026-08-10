namespace Fip.Application.Flights;

/// <summary>
/// Initial heuristic settings for takeoff detection.
/// </summary>
public sealed record TakeoffDetectionOptions
{
    public double MinimumTakeoffGroundspeedKnots { get; init; } = 80;

    public double MinimumAltitudeGainFeet { get; init; } = 300;

    public int MinimumPreTakeoffSamples { get; init; } = 2;

    public int SustainedClimbSamples { get; init; } = 4;

    public int MinimumPositiveAltitudeSteps { get; init; } = 2;

    public int MinimumAirborneSamplesAfterCandidate { get; init; } = 2;

    public double MinimumClimbRateFeetPerMinute { get; init; } = 300;

    /// <summary>
    /// Allows a trajectory that starts shortly after lift-off to provide takeoff evidence
    /// when the source contains no preceding ground-speed transition.
    /// </summary>
    public int InitialClimbSamples { get; init; } = 10;

    public double MaximumInitialAltitudeFeet { get; init; } = 1_000;
}
