using Audit.Infrastructure;
using BuildingBlocks.Scoping;
using BuildingBlocks.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);
builder.Services.AddServiceDefaults("audit-worker");
builder.Services.AddPlatformScoping();
builder.Services.AddAuditInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();
await builder.Build().RunAsync();

public sealed class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Audit worker heartbeat");
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
