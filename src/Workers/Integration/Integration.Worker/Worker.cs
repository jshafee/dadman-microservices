using MassTransit;

namespace Integration.Worker;

public class Worker(IBus bus, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Integration worker started at: {time}", DateTimeOffset.Now);
        while (!stoppingToken.IsCancellationRequested)
        {
            await bus.Publish(new WorkerHeartbeat(DateTimeOffset.UtcNow), stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}

public record WorkerHeartbeat(DateTimeOffset AtUtc);
