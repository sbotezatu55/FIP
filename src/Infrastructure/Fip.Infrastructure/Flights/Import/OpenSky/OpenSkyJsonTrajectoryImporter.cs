using System.Text.Json;
using Fip.Application.Flights.Import.OpenSky;

namespace Fip.Infrastructure.Flights.Import.OpenSky;

public sealed class OpenSkyJsonTrajectoryImporter : IOpenSkyTrajectoryImporter
{
    private static readonly JsonSerializerOptions SerializerOptions = new();

    public async Task<IReadOnlyList<OpenSkyTelemetryPointDto>> ImportAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        return await JsonSerializer.DeserializeAsync<List<OpenSkyTelemetryPointDto>>(
                   content,
                   SerializerOptions,
                   cancellationToken)
               ?? [];
    }

    public async Task<IReadOnlyList<OpenSkyTelemetryPointDto>> ImportAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("A trajectory file path is required.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("The trajectory file was not found.", filePath);
        }

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        return await ImportAsync(stream, cancellationToken);
    }
}
