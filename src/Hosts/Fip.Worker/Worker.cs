namespace Fip.Worker;

public sealed class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}
