namespace Fip.Application.Flights;

/// <summary>
/// Initial heuristic settings for Top-of-Descent detection.
/// </summary>
public sealed record TopOfDescentDetectionOptions
{
    public int MinimumCruiseSamples { get; init; } = 4;

    public double MaximumCruiseAltitudeVariationFeet { get; init; } = 400;

    public double CruiseVerticalRateToleranceFeetPerMinute { get; init; } = 250;

    public int DescentConfirmationSamples { get; init; } = 5;

    public TimeSpan MinimumDescentDuration { get; init; } = TimeSpan.FromMinutes(2);

    public double MinimumDescentAltitudeLossFeet { get; init; } = 1_000;

    public int MinimumNegativeVerticalRateSamples { get; init; } = 3;

    public double MinimumDescentVerticalRateFeetPerMinute { get; init; } = 200;

    public TimeSpan MaximumConfirmationGap { get; init; } = TimeSpan.FromSeconds(90);

    public int RecoveryConfirmationSamples { get; init; } = 3;

    public double MinimumRecoveryAltitudeGainFeet { get; init; } = 500;
}
