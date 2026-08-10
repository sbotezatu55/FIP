using System.Text.Json.Serialization;

namespace Fip.Application.Flights.Import.OpenSky;

public sealed class OpenSkyTelemetryPointDto
{
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; init; }

    [JsonPropertyName("icao24")]
    public string Icao24 { get; init; } = string.Empty;

    [JsonPropertyName("latitude")]
    public double? Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; init; }

    [JsonPropertyName("groundspeed")]
    public double? GroundSpeed { get; init; }

    [JsonPropertyName("track")]
    public double? Track { get; init; }

    [JsonPropertyName("vertical_rate")]
    public double? VerticalRate { get; init; }

    [JsonPropertyName("callsign")]
    public string? Callsign { get; init; }

    [JsonPropertyName("altitude")]
    public double? Altitude { get; init; }
}
