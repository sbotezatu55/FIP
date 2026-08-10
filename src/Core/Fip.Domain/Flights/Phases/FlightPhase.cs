namespace Fip.Domain.Flights.Phases;

/// <summary>
/// Initial operational phases used to describe a reconstructed flight trajectory.
/// </summary>
public enum FlightPhase
{
    Unknown,
    Ground,
    TakeoffRoll,
    InitialClimb,
    Climb,
    Cruise,
    Descent,
    Approach,
    LandingRoll
}
