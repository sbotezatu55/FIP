using Fip.Application.Flights.Import.AdsbIq;
using Parquet;
using Parquet.Schema;

namespace Fip.Infrastructure.Flights.Import.AdsbIq;

public sealed class AdsbIqParquetTelemetryImporter : IAdsbIqTelemetryImporter
{
    public async Task<IReadOnlyList<AdsbIqTelemetryRow>> ImportAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (!content.CanSeek)
        {
            var buffered = new MemoryStream();
            await content.CopyToAsync(buffered, cancellationToken);
            buffered.Position = 0;
            content = buffered;
        }

        await using var reader = await ParquetReader.CreateAsync(content, cancellationToken: cancellationToken);
        var fields = reader.Schema.GetDataFields();
        var fieldMap = fields.ToDictionary(field => field.Name, StringComparer.Ordinal);
        var rows = new List<AdsbIqTelemetryRow>();
        var lastStateByIcao24 = new Dictionary<string, AdsbIqTelemetryRow>(StringComparer.OrdinalIgnoreCase);

        for (var groupIndex = 0; groupIndex < reader.RowGroupCount; groupIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var group = reader.OpenRowGroupReader(groupIndex);
            var columns = new Dictionary<string, Array>(StringComparer.Ordinal);

            foreach (var name in RequiredColumns)
            {
                if (fieldMap.TryGetValue(name, out var field))
                {
                    columns[name] = await ReadColumnAsync(group, field);
                }
            }

            var count = group.RowCount;
            for (var index = 0; index < count; index++)
            {
                var rawRow = new AdsbIqTelemetryRow
                {
                    Timestamp = Read<DateTimeOffset>(columns, "ts", index),
                    Icao24 = Read<string>(columns, "hex", index) ?? string.Empty,
                    Callsign = Read<string>(columns, "flight", index),
                    Latitude = Read<double?>(columns, "lat", index),
                    Longitude = Read<double?>(columns, "lon", index),
                    BarometricAltitudeFeet = Read<int?>(columns, "alt_baro", index),
                    GeometricAltitudeFeet = Read<int?>(columns, "alt_geom", index),
                    GroundSpeedKnots = Read<int?>(columns, "gs", index),
                    TrackDegrees = Read<float?>(columns, "track", index),
                    BarometricRateFeetPerMinute = Read<int?>(columns, "baro_rate", index),
                    GeometricRateFeetPerMinute = Read<int?>(columns, "geom_rate", index),
                    Squawk = Read<string>(columns, "squawk", index),
                    EmitterCategory = Read<string>(columns, "category", index),
                    IsRemoved = Read<bool?>(columns, "is_removed", index) ?? false
                };

                // ADSBiq daily archives contain append-only sparse diffs: a null value
                // means that the previous value for this aircraft is unchanged.
                // Carry the last state forward before the application validates rows.
                if (rawRow.IsRemoved)
                {
                    rows.Add(rawRow);
                    lastStateByIcao24.Remove(rawRow.Icao24);
                    continue;
                }

                lastStateByIcao24.TryGetValue(rawRow.Icao24, out var previous);
                var normalizedRow = new AdsbIqTelemetryRow
                {
                    Timestamp = rawRow.Timestamp,
                    Icao24 = rawRow.Icao24,
                    Callsign = rawRow.Callsign ?? previous?.Callsign,
                    Latitude = rawRow.Latitude ?? previous?.Latitude,
                    Longitude = rawRow.Longitude ?? previous?.Longitude,
                    BarometricAltitudeFeet = rawRow.BarometricAltitudeFeet ?? previous?.BarometricAltitudeFeet,
                    GeometricAltitudeFeet = rawRow.GeometricAltitudeFeet ?? previous?.GeometricAltitudeFeet,
                    GroundSpeedKnots = rawRow.GroundSpeedKnots ?? previous?.GroundSpeedKnots,
                    TrackDegrees = rawRow.TrackDegrees ?? previous?.TrackDegrees,
                    BarometricRateFeetPerMinute = rawRow.BarometricRateFeetPerMinute ?? previous?.BarometricRateFeetPerMinute,
                    GeometricRateFeetPerMinute = rawRow.GeometricRateFeetPerMinute ?? previous?.GeometricRateFeetPerMinute,
                    Squawk = rawRow.Squawk ?? previous?.Squawk,
                    EmitterCategory = rawRow.EmitterCategory ?? previous?.EmitterCategory
                };

                rows.Add(normalizedRow);
                lastStateByIcao24[normalizedRow.Icao24] = normalizedRow;
            }
        }

        return rows;
    }

    private static readonly string[] RequiredColumns =
    [
        "ts", "hex", "flight", "lat", "lon", "alt_baro", "alt_geom", "gs", "track",
        "baro_rate", "geom_rate", "squawk", "category", "is_removed"
    ];

    private static T? Read<T>(IReadOnlyDictionary<string, Array> columns, string name, int index)
    {
        if (!columns.TryGetValue(name, out var column)) return default;
        var value = column.GetValue(index);
        return value is null ? default : (T?)ConvertValue(value, typeof(T));
    }

    private static async Task<Array> ReadColumnAsync(
        ParquetRowGroupReader group,
        DataField field)
    {
        Array values = field.Name switch
        {
            "ts" => new DateTime?[group.RowCount],
            "lat" or "lon" => new double?[group.RowCount],
            "track" => new float?[group.RowCount],
            "is_removed" => new bool?[group.RowCount],
            "flight" or "squawk" or "category" or "hex" => new string[group.RowCount],
            _ => new int?[group.RowCount]
        };

        if (values is DateTime?[] timestamps)
        {
            await group.ReadAsync(field, timestamps.AsMemory(), cancellationToken: default);
        }
        else if (values is double?[] doubles)
        {
            await group.ReadAsync(field, doubles.AsMemory(), cancellationToken: default);
        }
        else if (values is float?[] floats)
        {
            await group.ReadAsync(field, floats.AsMemory(), cancellationToken: default);
        }
        else if (values is bool?[] booleans)
        {
            await group.ReadAsync(field, booleans.AsMemory(), cancellationToken: default);
        }
        else if (values is string[] strings)
        {
            await group.ReadAsync(field, strings.AsMemory(), cancellationToken: default);
        }
        else if (values is int?[] integers)
        {
            await group.ReadAsync(field, integers.AsMemory(), cancellationToken: default);
        }

        return values;
    }

    private static object ConvertValue(object value, Type targetType)
    {
        var nullableType = Nullable.GetUnderlyingType(targetType);
        var actualType = nullableType ?? targetType;
        if (actualType == typeof(DateTimeOffset) && value is DateTime dateTime)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
        }
        if (actualType == typeof(DateTimeOffset) && value is DateTimeOffset dateTimeOffset) return dateTimeOffset;
        if (actualType == typeof(string)) return value.ToString() ?? string.Empty;
        return Convert.ChangeType(value, actualType, System.Globalization.CultureInfo.InvariantCulture)!;
    }
}
