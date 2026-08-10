using Fip.Application.Flights;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Tests;

public sealed class FlightReconstructorTests
{
    [Fact]
    public void Reconstruct_BuildsFlightFromNormalizedTelemetry()
    {
        var points = new[]
        {
            CreatePoint("2026-01-01T10:00:00Z", 10, 20, 10_000, " FIP123 "),
            CreatePoint("2026-01-01T10:01:00Z", 11, 21, 20_000, "FIP123")
        };

        var flight = new FlightReconstructor().Reconstruct(points);

        Assert.Equal("abc123", flight.Icao24);
        Assert.Equal("FIP123", flight.Callsign);
        Assert.Equal(2, flight.TelemetryPoints.Count);
    }

    [Fact]
    public void Reconstruct_SortsTelemetryWithoutMutatingInput()
    {
        var early = CreatePoint("2026-01-01T10:00:00Z", 10, 20, 10_000);
        var late = CreatePoint("2026-01-01T10:02:00Z", 12, 22, 30_000);
        var middle = CreatePoint("2026-01-01T10:01:00Z", 11, 21, 20_000);
        var points = new[] { late, early, middle };

        var flight = new FlightReconstructor().Reconstruct(points);

        Assert.Equal(new[] { late, early, middle }, points);
        Assert.Equal(new[] { early, middle, late }, flight.TelemetryPoints);
    }

    [Fact]
    public void Reconstruct_UsesTelemetryBoundsAndEndpointCoordinates()
    {
        var early = CreatePoint("2026-01-01T10:00:00Z", 10, 20, 10_000);
        var late = CreatePoint("2026-01-01T10:02:00Z", 12, 22, 30_000);

        var flight = new FlightReconstructor().Reconstruct(new[] { late, early });

        Assert.Equal(early.Timestamp, flight.StartTime);
        Assert.Equal(late.Timestamp, flight.EndTime);
        Assert.Equal(10, flight.DepartureLatitude);
        Assert.Equal(20, flight.DepartureLongitude);
        Assert.Equal(12, flight.ArrivalLatitude);
        Assert.Equal(22, flight.ArrivalLongitude);
    }

    [Fact]
    public void Reconstruct_CalculatesMaximumAltitudeAndAllowsMissingAltitude()
    {
        var points = new[]
        {
            CreatePoint("2026-01-01T10:00:00Z", altitudeFeet: null),
            CreatePoint("2026-01-01T10:01:00Z", altitudeFeet: 25_000),
            CreatePoint("2026-01-01T10:02:00Z", altitudeFeet: 10_000)
        };

        var flight = new FlightReconstructor().Reconstruct(points);

        Assert.Equal(25_000, flight.MaximumAltitudeFeet);
        Assert.Null(new FlightReconstructor().Reconstruct(new[]
        {
            CreatePoint("2026-01-01T10:00:00Z", altitudeFeet: null)
        }).MaximumAltitudeFeet);
    }

    [Fact]
    public void Reconstruct_RejectsInconsistentIcao24Values()
    {
        var points = new[]
        {
            CreatePoint("2026-01-01T10:00:00Z"),
            CreatePoint("2026-01-01T10:01:00Z", icao24: "other1")
        };

        var exception = Assert.Throws<ArgumentException>(() => new FlightReconstructor().Reconstruct(points));

        Assert.Contains("same ICAO24", exception.Message);
    }

    [Fact]
    public void Reconstruct_RejectsNullAndEmptyTelemetry()
    {
        Assert.Throws<ArgumentNullException>(() => new FlightReconstructor().Reconstruct(null!));
        Assert.Throws<ArgumentException>(() => new FlightReconstructor().Reconstruct(Array.Empty<FlightTelemetryPoint>()));
    }

    [Fact]
    public void Reconstruct_SelectsMostFrequentNormalizedCallsign()
    {
        var points = new[]
        {
            CreatePoint("2026-01-01T10:00:00Z", callsign: "  FIRST "),
            CreatePoint("2026-01-01T10:01:00Z", callsign: "   "),
            CreatePoint("2026-01-01T10:02:00Z", callsign: "SECOND"),
            CreatePoint("2026-01-01T10:03:00Z", callsign: "SECOND"),
            CreatePoint("2026-01-01T10:04:00Z", callsign: null)
        };

        var flight = new FlightReconstructor().Reconstruct(points);

        Assert.Equal("SECOND", flight.Callsign);
    }

    [Fact]
    public void Reconstruct_ResolvesCallsignTieByEarliestOccurrence()
    {
        var points = new[]
        {
            CreatePoint("2026-01-01T10:00:00Z", callsign: "EARLY"),
            CreatePoint("2026-01-01T10:01:00Z", callsign: "LATE")
        };

        var flight = new FlightReconstructor().Reconstruct(points);

        Assert.Equal("EARLY", flight.Callsign);
    }

    [Fact]
    public void Reconstruct_ReturnsNullCallsignWhenNoUsableCallsignExists()
    {
        var points = new[]
        {
            CreatePoint("2026-01-01T10:00:00Z", callsign: null),
            CreatePoint("2026-01-01T10:01:00Z", callsign: "  ")
        };

        var flight = new FlightReconstructor().Reconstruct(points);

        Assert.Null(flight.Callsign);
    }

    private static FlightTelemetryPoint CreatePoint(
        string timestamp = "2026-01-01T10:00:00Z",
        double? latitude = 1,
        double? longitude = 2,
        double? altitudeFeet = 1_000,
        string? callsign = "FIP123",
        string icao24 = "abc123") => new()
    {
        Timestamp = DateTimeOffset.Parse(timestamp),
        Icao24 = icao24,
        Callsign = callsign,
        Latitude = latitude,
        Longitude = longitude,
        AltitudeFeet = altitudeFeet
    };
}
