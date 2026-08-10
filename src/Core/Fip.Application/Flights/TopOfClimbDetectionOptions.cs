namespace Fip.Application.Flights;

/// <summary>
/// Initial heuristic settings for Top-of-Climb detection.
/// </summary>
public sealed record TopOfClimbDetectionOptions
{
    public int MinimumClimbSamples { get; init; } = 4;

    public TimeSpan MinimumClimbDuration { get; init; } = TimeSpan.FromMinutes(1);

    public double MinimumClimbAltitudeGainFeet { get; init; } = 1_000;

    public double MinimumClimbVerticalRateFeetPerMinute { get; init; } = 200;

    public int LevelConfirmationSamples { get; init; } = 5;

    public TimeSpan MinimumLevelDuration { get; init; } = TimeSpan.FromMinutes(2);

    public double MaximumLevelAltitudeVariationFeet { get; init; } = 400;

    public double LevelVerticalRateToleranceFeetPerMinute { get; init; } = 250;

    public TimeSpan MaximumConfirmationGap { get; init; } = TimeSpan.FromSeconds(90);
}
