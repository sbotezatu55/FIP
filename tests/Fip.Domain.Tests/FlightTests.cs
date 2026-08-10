using Fip.Domain.Flights;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Domain.Tests;

public sealed class FlightTests
{
    [Fact]
    public void Constructor_StoresFlightMetadataAndTelemetryPoints()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var telemetryPoint = CreateTelemetryPoint(timestamp);

        var flight = CreateFlight(new[] { telemetryPoint }, startTime: timestamp);

        Assert.NotEqual(Guid.Empty, flight.Id);
        Assert.Equal("abc123", flight.Icao24);
        Assert.Equal("FIP123", flight.Callsign);
        Assert.Equal(timestamp, flight.StartTime);
        Assert.Equal(timestamp.AddMinutes(10), flight.EndTime);
        Assert.Equal(51.0, flight.DepartureLatitude);
        Assert.Equal(-0.1, flight.DepartureLongitude);
        Assert.Equal(52.0, flight.ArrivalLatitude);
        Assert.Equal(0.2, flight.ArrivalLongitude);
        Assert.Equal(35_000, flight.MaximumAltitudeFeet);
        Assert.Single(flight.TelemetryPoints);
        Assert.Same(telemetryPoint, flight.TelemetryPoints[0]);
    }

    [Fact]
    public void Constructor_AllowsMissingCallsign()
    {
        var flight = CreateFlight(callsign: null);

        Assert.Null(flight.Callsign);
    }

    [Fact]
    public void Constructor_SnapshotsTelemetryPoints()
    {
        var telemetryPoints = new List<FlightTelemetryPoint>
        {
            CreateTelemetryPoint(DateTimeOffset.UtcNow)
        };

        var flight = CreateFlight(telemetryPoints);
        telemetryPoints.Add(CreateTelemetryPoint(DateTimeOffset.UtcNow.AddMinutes(1)));

        Assert.Single(flight.TelemetryPoints);
    }

    [Fact]
    public void TelemetryPoints_CannotBeMutatedThroughPublicCollection()
    {
        var flight = CreateFlight(new[] { CreateTelemetryPoint(DateTimeOffset.UtcNow) });

        var collection = Assert.IsAssignableFrom<IList<FlightTelemetryPoint>>(flight.TelemetryPoints);

        Assert.Throws<NotSupportedException>(() => collection.Add(CreateTelemetryPoint(DateTimeOffset.UtcNow)));
        Assert.Single(flight.TelemetryPoints);
    }

    private static Flight CreateFlight(
        IEnumerable<FlightTelemetryPoint>? telemetryPoints = null,
        string? callsign = "FIP123",
        DateTimeOffset? startTime = null)
    {
        var flightStartTime = startTime ?? DateTimeOffset.UtcNow;

        return new Flight(
            "abc123",
            callsign,
            flightStartTime,
            flightStartTime.AddMinutes(10),
            51.0,
            -0.1,
            52.0,
            0.2,
            35_000,
            telemetryPoints ?? Array.Empty<FlightTelemetryPoint>());
    }

    private static FlightTelemetryPoint CreateTelemetryPoint(DateTimeOffset timestamp) => new()
    {
        Timestamp = timestamp,
        Icao24 = "abc123",
        Latitude = 51.0,
        Longitude = -0.1,
        AltitudeFeet = 10_000
    };
}
