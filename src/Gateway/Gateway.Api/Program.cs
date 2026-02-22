using BuildingBlocks.Security;
using BuildingBlocks.ServiceDefaults;
using Gateway.Api;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, loggerConfiguration) =>
{
    var seqServerUrl = context.Configuration["Seq:ServerUrl"];
    var seqApiKey = context.Configuration["Seq:ApiKey"];

    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProperty("service.name", "gateway-api")
        .WriteTo.Console();

    if (!string.IsNullOrWhiteSpace(seqServerUrl))
    {
        loggerConfiguration.WriteTo.Seq(seqServerUrl, apiKey: seqApiKey);
    }
});

builder.Services.AddServiceDefaults("gateway-api");
builder.Services.AddJwtSecurity(builder.Configuration);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("fixed", context =>
    {
        var key = GetRateLimitPartitionKey(context);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"fixed:{key}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 20,
                AutoReplenishment = true
            });
    });

    options.AddPolicy("write", context =>
    {
        var key = GetRateLimitPartitionKey(context);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"write:{key}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5,
                AutoReplenishment = true
            });
    });
});

builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("catalog-get", CatalogOutputCachePolicy.Instance);
});

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();
app.UseServiceDefaults();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseOutputCache();
app.MapReverseProxy();
app.Run();

static string GetRateLimitPartitionKey(HttpContext context)
{
    var subject = context.User.FindFirst("sub")?.Value;
    if (!string.IsNullOrWhiteSpace(subject))
    {
        return $"user:{subject}";
    }

    var nameIdentifier = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!string.IsNullOrWhiteSpace(nameIdentifier))
    {
        return $"user:{nameIdentifier}";
    }

    var ipAddress = context.Connection.RemoteIpAddress?.ToString();
    return string.IsNullOrWhiteSpace(ipAddress) ? "ip:unknown" : $"ip:{ipAddress}";
}

public partial class Program { }

namespace Gateway.Api
{
    public sealed class GatewayApiMarker { }
}
