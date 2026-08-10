using Fip.Domain.Flights.Telemetry;
using Fip.Domain.FlightEvents;
using Fip.SharedKernel;

namespace Fip.Domain.Flights;

/// <summary>
/// Represents one reconstructed, source-independent aircraft flight.
/// </summary>
public sealed class Flight : Entity
{
    private readonly IReadOnlyList<FlightTelemetryPoint> _telemetryPoints;
    private readonly List<FlightEvent> _events = new();
    private readonly IReadOnlyList<FlightEvent> _readOnlyEvents;

    public Flight(
        string icao24,
        string? callsign,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        double? departureLatitude,
        double? departureLongitude,
        double? arrivalLatitude,
        double? arrivalLongitude,
        double? maximumAltitudeFeet,
        IEnumerable<FlightTelemetryPoint> telemetryPoints)
    {
        ArgumentNullException.ThrowIfNull(icao24);
        ArgumentNullException.ThrowIfNull(telemetryPoints);

        Icao24 = icao24;
        Callsign = callsign;
        StartTime = startTime;
        EndTime = endTime;
        DepartureLatitude = departureLatitude;
        DepartureLongitude = departureLongitude;
        ArrivalLatitude = arrivalLatitude;
        ArrivalLongitude = arrivalLongitude;
        MaximumAltitudeFeet = maximumAltitudeFeet;
        _telemetryPoints = telemetryPoints.ToList().AsReadOnly();
        _readOnlyEvents = _events.AsReadOnly();
    }

    /// <summary>
    /// Reconstitutes a flight with its persisted domain identifier.
    /// </summary>
    public static Flight Reconstitute(
        Guid id,
        string icao24,
        string? callsign,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        double? departureLatitude,
        double? departureLongitude,
        double? arrivalLatitude,
        double? arrivalLongitude,
        double? maximumAltitudeFeet,
        IEnumerable<FlightTelemetryPoint> telemetryPoints)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("The flight identifier cannot be empty.", nameof(id));
        }

        return new Flight(
            icao24,
            callsign,
            startTime,
            endTime,
            departureLatitude,
            departureLongitude,
            arrivalLatitude,
            arrivalLongitude,
            maximumAltitudeFeet,
            telemetryPoints)
        {
            Id = id
        };
    }

    public string Icao24 { get; }

    public string? Callsign { get; }

    public DateTimeOffset StartTime { get; }

    public DateTimeOffset EndTime { get; }

    public double? DepartureLatitude { get; }

    public double? DepartureLongitude { get; }

    public double? ArrivalLatitude { get; }

    public double? ArrivalLongitude { get; }

    public double? MaximumAltitudeFeet { get; }

    public IReadOnlyList<FlightTelemetryPoint> TelemetryPoints => _telemetryPoints;

    public IReadOnlyList<FlightEvent> Events => _readOnlyEvents;

    public void AddEvent(FlightEvent flightEvent)
    {
        ArgumentNullException.ThrowIfNull(flightEvent);

        _events.Add(flightEvent);
    }
}
