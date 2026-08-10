namespace Fip.Domain.Flights.Telemetry;

public sealed class TelemetryValidationResult
{
    public TelemetryValidationResult(
        TelemetryValidationStatus status,
        IEnumerable<TelemetryValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        Status = status;
        Issues = issues.ToList().AsReadOnly();
    }

    public TelemetryValidationStatus Status { get; }

    public IReadOnlyList<TelemetryValidationIssue> Issues { get; }
}
