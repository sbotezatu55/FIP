namespace Fip.Domain.Flights;

/// <summary>
/// Statistics calculated from a reconstructed flight and its detected events.
/// </summary>
public sealed class FlightSummary
{
    public TimeSpan Duration { get; init; }

    public double DistanceNauticalMiles { get; init; }

    public double? MaximumAltitudeFeet { get; init; }

    public double? MaximumGroundSpeedKnots { get; init; }

    public double? AverageGroundSpeedKnots { get; init; }

    public double? MaximumClimbRate { get; init; }

    public double? MaximumDescentRate { get; init; }

    public DateTimeOffset? TakeoffTime { get; init; }

    public DateTimeOffset? LandingTime { get; init; }

    public TimeSpan? FlightTime { get; init; }
}
