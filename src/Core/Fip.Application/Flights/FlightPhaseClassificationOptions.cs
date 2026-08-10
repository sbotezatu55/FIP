namespace Fip.Application.Flights;

/// <summary>
/// Initial heuristic settings for deterministic flight-phase classification.
/// </summary>
public sealed record FlightPhaseClassificationOptions
{
    public double GroundGroundSpeedThresholdKnots { get; init; } = 80;

    public double GroundVerticalRateToleranceFeetPerMinute { get; init; } = 250;

    public double TakeoffRollMinimumGroundSpeedKnots { get; init; } = 60;

    public TimeSpan InitialClimbDuration { get; init; } = TimeSpan.FromMinutes(2);

    public double InitialClimbAltitudeGainFeet { get; init; } = 3_000;

    public double MinimumClimbVerticalRateFeetPerMinute { get; init; } = 200;

    public double MinimumDescentVerticalRateFeetPerMinute { get; init; } = 200;

    public double MinimumAltitudeTrendFeet { get; init; } = 300;

    public double LevelVerticalRateToleranceFeetPerMinute { get; init; } = 250;

    public double CruiseAltitudeVariationFeet { get; init; } = 500;

    public TimeSpan ApproachWindow { get; init; } = TimeSpan.FromMinutes(2);

    public TimeSpan MaximumContinuousTelemetryGap { get; init; } = TimeSpan.FromSeconds(90);
}
