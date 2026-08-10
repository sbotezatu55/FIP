using Fip.Application.Abstractions.Flights;
using Fip.Domain.Flights;
using Fip.Domain.Flights.Telemetry;
using Fip.Domain.FlightEvents;
using Fip.SharedKernel.Geography;

namespace Fip.Application.Flights;

/// <summary>
/// Calculates statistics from normalized flight telemetry and detected events.
/// </summary>
public sealed class FlightSummaryCalculator : IFlightSummaryCalculator
{
    private readonly IGeoDistanceCalculator _geoDistanceCalculator;

    public FlightSummaryCalculator(IGeoDistanceCalculator geoDistanceCalculator)
    {
        ArgumentNullException.ThrowIfNull(geoDistanceCalculator);

        _geoDistanceCalculator = geoDistanceCalculator;
    }

    public FlightSummary Calculate(Flight flight)
    {
        ArgumentNullException.ThrowIfNull(flight);

        var altitudeValues = flight.TelemetryPoints
            .Select(point => point.AltitudeFeet)
            .Where(value => value.HasValue && double.IsFinite(value.Value))
            .Select(value => value!.Value)
            .ToList();

        var groundSpeedValues = flight.TelemetryPoints
            .Select(point => point.GroundSpeedKnots)
            .Where(value => value.HasValue && double.IsFinite(value.Value) && value.Value >= 0)
            .Select(value => value!.Value)
            .ToList();

        var verticalRateValues = flight.TelemetryPoints
            .Select(point => point.VerticalRateFeetPerMinute)
            .Where(value => value.HasValue && double.IsFinite(value.Value))
            .Select(value => value!.Value)
            .ToList();

        var takeoffTime = flight.Events
            .Where(flightEvent => flightEvent.Type == FlightEventType.Takeoff)
            .OrderBy(flightEvent => flightEvent.Timestamp)
            .Select(flightEvent => (DateTimeOffset?)flightEvent.Timestamp)
            .FirstOrDefault();

        var landingTime = flight.Events
            .Where(flightEvent => flightEvent.Type == FlightEventType.Landing)
            .OrderBy(flightEvent => flightEvent.Timestamp)
            .Select(flightEvent => (DateTimeOffset?)flightEvent.Timestamp)
            .FirstOrDefault();

        return new FlightSummary
        {
            Duration = flight.EndTime - flight.StartTime,
            DistanceNauticalMiles = CalculateDistanceNauticalMiles(flight.TelemetryPoints),
            MaximumAltitudeFeet = altitudeValues.Count == 0 ? null : altitudeValues.Max(),
            MaximumGroundSpeedKnots = groundSpeedValues.Count == 0 ? null : groundSpeedValues.Max(),
            AverageGroundSpeedKnots = groundSpeedValues.Count == 0 ? null : groundSpeedValues.Average(),
            MaximumClimbRate = verticalRateValues
                .Where(value => value > 0)
                .Select(value => (double?)value)
                .Max(),
            MaximumDescentRate = verticalRateValues
                .Where(value => value < 0)
                .Select(value => (double?)-value)
                .Max(),
            TakeoffTime = takeoffTime,
            LandingTime = landingTime,
            FlightTime = takeoffTime.HasValue && landingTime.HasValue
                ? landingTime.Value - takeoffTime.Value
                : null
        };
    }

    private double CalculateDistanceNauticalMiles(IReadOnlyList<FlightTelemetryPoint> telemetryPoints)
    {
        var distance = 0d;
        FlightTelemetryPoint? previousPoint = null;

        foreach (var point in telemetryPoints.OrderBy(point => point.Timestamp))
        {
            if (!HasUsablePosition(point))
            {
                previousPoint = null;
                continue;
            }

            if (previousPoint is not null)
            {
                distance += _geoDistanceCalculator.CalculateNauticalMiles(
                    previousPoint.Latitude!.Value,
                    previousPoint.Longitude!.Value,
                    point.Latitude!.Value,
                    point.Longitude!.Value);
            }

            previousPoint = point;
        }

        return distance;
    }

    private static bool HasUsablePosition(FlightTelemetryPoint point) =>
        point.Latitude is { } latitude &&
        point.Longitude is { } longitude &&
        double.IsFinite(latitude) &&
        double.IsFinite(longitude) &&
        latitude is >= -90 and <= 90 &&
        longitude is >= -180 and <= 180;
}
