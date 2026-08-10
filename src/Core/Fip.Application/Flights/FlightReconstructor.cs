using Fip.Application.Abstractions.Flights;
using Fip.Domain.Flights;
using Fip.Domain.Flights.Telemetry;

namespace Fip.Application.Flights;

/// <summary>
/// Reconstructs a source-independent <see cref="Flight"/> aggregate from normalized telemetry points.
/// </summary>
public sealed class FlightReconstructor : IFlightReconstructor
{
    public Flight Reconstruct(IReadOnlyList<FlightTelemetryPoint> telemetryPoints)
    {
        ArgumentNullException.ThrowIfNull(telemetryPoints);

        if (telemetryPoints.Count == 0)
        {
            throw new ArgumentException("At least one telemetry point is required.", nameof(telemetryPoints));
        }

        var orderedTelemetryPoints = telemetryPoints
            .Select((point, index) => (Point: point, Index: index))
            .OrderBy(item => item.Point?.Timestamp)
            .ThenBy(item => item.Index)
            .Select(item => item.Point ?? throw new ArgumentException(
                "Telemetry points cannot contain null values.",
                nameof(telemetryPoints)))
            .ToList();

        ValidateIcao24Consistency(orderedTelemetryPoints);

        var firstPoint = orderedTelemetryPoints[0];
        var lastPoint = orderedTelemetryPoints[^1];

        return new Flight(
            firstPoint.Icao24,
            DetermineCallsign(orderedTelemetryPoints),
            firstPoint.Timestamp,
            lastPoint.Timestamp,
            firstPoint.Latitude,
            firstPoint.Longitude,
            lastPoint.Latitude,
            lastPoint.Longitude,
            CalculateMaximumAltitude(orderedTelemetryPoints),
            orderedTelemetryPoints);
    }

    private static void ValidateIcao24Consistency(IReadOnlyList<FlightTelemetryPoint> telemetryPoints)
    {
        var icao24 = telemetryPoints[0].Icao24;

        if (telemetryPoints.Any(point => !string.Equals(point.Icao24, icao24, StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "All telemetry points must belong to the same ICAO24 aircraft.",
                nameof(telemetryPoints));
        }
    }

    private static string? DetermineCallsign(IReadOnlyList<FlightTelemetryPoint> telemetryPoints)
    {
        var callsignOccurrences = new Dictionary<string, (int Count, int FirstIndex)>(StringComparer.Ordinal);

        for (var index = 0; index < telemetryPoints.Count; index++)
        {
            var callsign = telemetryPoints[index].Callsign?.Trim();

            if (string.IsNullOrWhiteSpace(callsign))
            {
                continue;
            }

            if (callsignOccurrences.TryGetValue(callsign, out var occurrence))
            {
                callsignOccurrences[callsign] = (occurrence.Count + 1, occurrence.FirstIndex);
            }
            else
            {
                callsignOccurrences[callsign] = (1, index);
            }
        }

        return callsignOccurrences
            .OrderByDescending(item => item.Value.Count)
            .ThenBy(item => item.Value.FirstIndex)
            .Select(item => item.Key)
            .FirstOrDefault();
    }

    private static double? CalculateMaximumAltitude(IReadOnlyList<FlightTelemetryPoint> telemetryPoints) =>
        telemetryPoints
            .Where(point => point.AltitudeFeet.HasValue)
            .Select(point => point.AltitudeFeet)
            .DefaultIfEmpty()
            .Max();
}
