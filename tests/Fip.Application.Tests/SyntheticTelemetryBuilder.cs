using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Tests;

internal sealed class SyntheticTelemetryBuilder
{
    private readonly DateTimeOffset _startTimestamp;
    private readonly List<FlightTelemetryPoint> _points = new();
    private string _icao24 = "abc123";
    private string? _callsign = "FIP123";
    private TimeSpan _samplingInterval = TimeSpan.FromSeconds(1);

    public SyntheticTelemetryBuilder(
        DateTimeOffset? startTimestamp = null,
        string icao24 = "abc123",
        string? callsign = "FIP123")
    {
        _startTimestamp = startTimestamp ?? new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        _icao24 = icao24;
        _callsign = callsign;
    }

    public SyntheticTelemetryBuilder WithSamplingInterval(TimeSpan samplingInterval)
    {
        _samplingInterval = samplingInterval;
        return this;
    }

    public SyntheticTelemetryBuilder WithIcao24(string icao24)
    {
        _icao24 = icao24;
        return this;
    }

    public SyntheticTelemetryBuilder WithCallsign(string? callsign)
    {
        _callsign = callsign;
        return this;
    }

    public SyntheticTelemetryBuilder AtSample(
        int sample,
        double? altitudeFeet,
        double? groundSpeedKnots,
        double? verticalRateFeetPerMinute,
        double? latitude = 28.5,
        double? longitude = -81.3)
    {
        return AtOffset(
            sample * _samplingInterval,
            altitudeFeet,
            groundSpeedKnots,
            verticalRateFeetPerMinute,
            latitude,
            longitude);
    }

    public SyntheticTelemetryBuilder AtOffset(
        TimeSpan offset,
        double? altitudeFeet,
        double? groundSpeedKnots,
        double? verticalRateFeetPerMinute,
        double? latitude = 28.5,
        double? longitude = -81.3)
    {
        _points.Add(new FlightTelemetryPoint
        {
            Timestamp = _startTimestamp + offset,
            Icao24 = _icao24,
            Callsign = _callsign,
            Latitude = latitude,
            Longitude = longitude,
            AltitudeFeet = altitudeFeet,
            GroundSpeedKnots = groundSpeedKnots,
            VerticalRateFeetPerMinute = verticalRateFeetPerMinute
        });

        return this;
    }

    public IReadOnlyList<FlightTelemetryPoint> Build() => _points.ToList();
}
