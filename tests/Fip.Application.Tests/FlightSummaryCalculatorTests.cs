using Fip.Application.Flights;
using Fip.Domain.Flights;
using Fip.Domain.Flights.Telemetry;
using Fip.Domain.FlightEvents;
using Fip.SharedKernel.Geography;

namespace Fip.Application.Tests;

public sealed class FlightSummaryCalculatorTests
{
    private static readonly DateTimeOffset StartTime = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Calculate_UsesFlightBoundsForDuration()
    {
        var flight = CreateFlight(StartTime, StartTime.AddMinutes(45), Array.Empty<FlightTelemetryPoint>());

        var summary = new FlightSummaryCalculator(new GeoDistanceCalculator()).Calculate(flight);

        Assert.Equal(TimeSpan.FromMinutes(45), summary.Duration);
    }

    [Fact]
    public void Calculate_ReturnsMaximumAltitude()
    {
        var flight = CreateFlightWithPoints(Point(10_000), Point(32_000), Point(25_000));

        Assert.Equal(32_000, new FlightSummaryCalculator(new GeoDistanceCalculator()).Calculate(flight).MaximumAltitudeFeet);
    }

    [Fact]
    public void Calculate_ReturnsMaximumAndAverageGroundspeed()
    {
        var flight = CreateFlightWithPoints(Point(groundSpeedKnots: 100), Point(groundSpeedKnots: 250), Point(groundSpeedKnots: 150));

        var summary = new FlightSummaryCalculator(new GeoDistanceCalculator()).Calculate(flight);

        Assert.Equal(250, summary.MaximumGroundSpeedKnots);
        Assert.Equal(166.66666666666666, summary.AverageGroundSpeedKnots);
    }

    [Fact]
    public void Calculate_ReturnsMaximumClimbAndDescentMagnitude()
    {
        var flight = CreateFlightWithPoints(
            Point(verticalRateFeetPerMinute: 500),
            Point(verticalRateFeetPerMinute: 1_200),
            Point(verticalRateFeetPerMinute: -500),
            Point(verticalRateFeetPerMinute: -1_800),
            Point(verticalRateFeetPerMinute: -1_200));

        var summary = new FlightSummaryCalculator(new GeoDistanceCalculator()).Calculate(flight);

        Assert.Equal(1_200, summary.MaximumClimbRate);
        Assert.Equal(1_800, summary.MaximumDescentRate);
    }

    [Fact]
    public void Calculate_ExtractsTakeoffLandingAndFlightTime()
    {
        var takeoff = StartTime.AddMinutes(10);
        var landing = StartTime.AddMinutes(40);
        var flight = CreateFlightWithPoints();
        flight.AddEvent(new FlightEvent(FlightEventType.Takeoff, takeoff));
        flight.AddEvent(new FlightEvent(FlightEventType.Landing, landing));

        var summary = new FlightSummaryCalculator(new GeoDistanceCalculator()).Calculate(flight);

        Assert.Equal(takeoff, summary.TakeoffTime);
        Assert.Equal(landing, summary.LandingTime);
        Assert.Equal(TimeSpan.FromMinutes(30), summary.FlightTime);
    }

    [Fact]
    public void Calculate_ReturnsNullForMissingEventsAndNullableTelemetry()
    {
        var flight = CreateFlightWithPoints(
            Point(),
            Point(altitudeFeet: 10_000, groundSpeedKnots: 200, verticalRateFeetPerMinute: null));

        var summary = new FlightSummaryCalculator(new GeoDistanceCalculator()).Calculate(flight);

        Assert.Equal(10_000, summary.MaximumAltitudeFeet);
        Assert.Equal(200, summary.MaximumGroundSpeedKnots);
        Assert.Equal(200, summary.AverageGroundSpeedKnots);
        Assert.Null(summary.MaximumClimbRate);
        Assert.Null(summary.MaximumDescentRate);
        Assert.Null(summary.TakeoffTime);
        Assert.Null(summary.LandingTime);
        Assert.Null(summary.FlightTime);
    }

    [Fact]
    public void Calculate_ReturnsNullFlightTimeWhenOnlyOneEventIsPresent()
    {
        var flight = CreateFlightWithPoints();
        flight.AddEvent(new FlightEvent(FlightEventType.Takeoff, StartTime.AddMinutes(10)));

        var summary = new FlightSummaryCalculator(new GeoDistanceCalculator()).Calculate(flight);

        Assert.NotNull(summary.TakeoffTime);
        Assert.Null(summary.LandingTime);
        Assert.Null(summary.FlightTime);
    }

    [Fact]
    public void Calculate_ReturnsNullStatisticsForEmptyTelemetry()
    {
        var flight = CreateFlight(StartTime, StartTime, Array.Empty<FlightTelemetryPoint>());

        var summary = new FlightSummaryCalculator(new GeoDistanceCalculator()).Calculate(flight);

        Assert.Null(summary.MaximumAltitudeFeet);
        Assert.Null(summary.MaximumGroundSpeedKnots);
        Assert.Null(summary.AverageGroundSpeedKnots);
        Assert.Null(summary.MaximumClimbRate);
        Assert.Null(summary.MaximumDescentRate);
        Assert.Equal(0, summary.DistanceNauticalMiles, precision: 10);
    }

    [Fact]
    public void Calculate_ReturnsZeroDistanceForOneTelemetryPoint()
    {
        var flight = CreateFlightWithPoints(Point(latitude: 0, longitude: 0));

        var summary = new FlightSummaryCalculator(new GeoDistanceCalculator()).Calculate(flight);

        Assert.Equal(0, summary.DistanceNauticalMiles, precision: 10);
    }

    [Fact]
    public void Calculate_ReturnsDistanceForTwoKnownPoints()
    {
        var flight = CreateFlightWithPoints(
            Point(timestampOffset: TimeSpan.Zero, latitude: 0, longitude: 0),
            Point(timestampOffset: TimeSpan.FromMinutes(1), latitude: 1, longitude: 0));

        var summary = new FlightSummaryCalculator(new GeoDistanceCalculator()).Calculate(flight);

        Assert.InRange(summary.DistanceNauticalMiles, 59.9, 60.1);
    }

    [Fact]
    public void Calculate_SumsDistanceAcrossThreeTelemetryPoints()
    {
        var flight = CreateFlightWithPoints(
            Point(timestampOffset: TimeSpan.Zero, latitude: 0, longitude: 0),
            Point(timestampOffset: TimeSpan.FromMinutes(1), latitude: 1, longitude: 0),
            Point(timestampOffset: TimeSpan.FromMinutes(2), latitude: 1, longitude: 1));

        var summary = new FlightSummaryCalculator(new GeoDistanceCalculator()).Calculate(flight);

        Assert.InRange(summary.DistanceNauticalMiles, 119.9, 120.2);
    }

    [Fact]
    public void Calculate_OrdersTelemetryChronologicallyBeforeSummingDistance()
    {
        var first = Point(timestampOffset: TimeSpan.Zero, latitude: 0, longitude: 0);
        var second = Point(timestampOffset: TimeSpan.FromMinutes(1), latitude: 1, longitude: 0);
        var third = Point(timestampOffset: TimeSpan.FromMinutes(2), latitude: 1, longitude: 1);
        var flight = CreateFlightWithPoints(third, first, second);

        var summary = new FlightSummaryCalculator(new GeoDistanceCalculator()).Calculate(flight);

        Assert.InRange(summary.DistanceNauticalMiles, 119.9, 120.2);
    }

    [Fact]
    public void Calculate_HandlesDuplicateTimestampsDeterministically()
    {
        var flight = CreateFlightWithPoints(
            Point(timestampOffset: TimeSpan.Zero, latitude: 0, longitude: 0),
            Point(timestampOffset: TimeSpan.Zero, latitude: 1, longitude: 0),
            Point(timestampOffset: TimeSpan.FromMinutes(1), latitude: 2, longitude: 0));

        var summary = new FlightSummaryCalculator(new GeoDistanceCalculator()).Calculate(flight);

        Assert.InRange(summary.DistanceNauticalMiles, 119.9, 120.2);
    }

    [Fact]
    public void Calculate_ReturnsZeroForDuplicateCoordinates()
    {
        var flight = CreateFlightWithPoints(
            Point(timestampOffset: TimeSpan.Zero, latitude: 10, longitude: 20),
            Point(timestampOffset: TimeSpan.FromMinutes(1), latitude: 10, longitude: 20));

        var summary = new FlightSummaryCalculator(new GeoDistanceCalculator()).Calculate(flight);

        Assert.Equal(0, summary.DistanceNauticalMiles, precision: 10);
    }

    [Fact]
    public void Calculate_DoesNotBridgeMissingCoordinate()
    {
        var flight = CreateFlightWithPoints(
            Point(timestampOffset: TimeSpan.Zero, latitude: 0, longitude: 0),
            Point(timestampOffset: TimeSpan.FromMinutes(1), latitude: null, longitude: 0),
            Point(timestampOffset: TimeSpan.FromMinutes(2), latitude: 1, longitude: 0));

        var summary = new FlightSummaryCalculator(new GeoDistanceCalculator()).Calculate(flight);

        Assert.Equal(0, summary.DistanceNauticalMiles, precision: 10);
    }

    [Fact]
    public void Calculate_DoesNotBridgeInvalidCoordinateAndResumesAfterIt()
    {
        var flight = CreateFlightWithPoints(
            Point(timestampOffset: TimeSpan.Zero, latitude: 0, longitude: 0),
            Point(timestampOffset: TimeSpan.FromMinutes(1), latitude: 91, longitude: 0),
            Point(timestampOffset: TimeSpan.FromMinutes(2), latitude: 1, longitude: 0),
            Point(timestampOffset: TimeSpan.FromMinutes(3), latitude: 2, longitude: 0));

        var summary = new FlightSummaryCalculator(new GeoDistanceCalculator()).Calculate(flight);

        Assert.InRange(summary.DistanceNauticalMiles, 59.9, 60.1);
    }

    [Fact]
    public void Calculate_RejectsNullFlight()
    {
        Assert.Throws<ArgumentNullException>(() => new FlightSummaryCalculator(new GeoDistanceCalculator()).Calculate(null!));
    }

    private static Flight CreateFlightWithPoints(params FlightTelemetryPoint[] points)
    {
        var orderedPoints = points.Length == 0
            ? new[] { Point() }
            : points;

        return CreateFlight(orderedPoints[0].Timestamp, orderedPoints[^1].Timestamp, points);
    }

    private static Flight CreateFlight(
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        IEnumerable<FlightTelemetryPoint> points) => new(
            "abc123",
            "FIP123",
            startTime,
            endTime,
            null,
            null,
            null,
            null,
            null,
            points);

    private static FlightTelemetryPoint Point(
        double? altitudeFeet = null,
        double? groundSpeedKnots = null,
        double? verticalRateFeetPerMinute = null,
        TimeSpan? timestampOffset = null,
        double? latitude = 28.5,
        double? longitude = -81.3) => new()
    {
        Timestamp = StartTime + (timestampOffset ?? TimeSpan.Zero),
        Icao24 = "abc123",
        Latitude = latitude,
        Longitude = longitude,
        AltitudeFeet = altitudeFeet,
        GroundSpeedKnots = groundSpeedKnots,
        VerticalRateFeetPerMinute = verticalRateFeetPerMinute
    };
}
