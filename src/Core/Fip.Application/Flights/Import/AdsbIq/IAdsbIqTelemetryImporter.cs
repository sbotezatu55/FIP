namespace Fip.Application.Flights.Import.AdsbIq;

public interface IAdsbIqTelemetryImporter
{
    Task<IReadOnlyList<AdsbIqTelemetryRow>> ImportAsync(Stream content, CancellationToken cancellationToken = default);
}
