using BuildingBlocks.Security;
using BuildingBlocks.ServiceDefaults;
using Gateway.Api;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using Serilog.Enrichers.Span;
using StackExchange.Redis;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);

builder.Host.UseSerilog((context, _, loggerConfiguration) =>
{
    var seqServerUrl = context.Configuration["Seq:ServerUrl"];
    var seqApiKey = context.Configuration["Seq:ApiKey"];

    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithSpan()
        .Enrich.WithProperty("service.name", "gateway-api")
        .WriteTo.Console();

    if (!string.IsNullOrWhiteSpace(seqServerUrl))
    {
        loggerConfiguration.WriteTo.Seq(seqServerUrl, apiKey: seqApiKey);
    }
});

builder.Services.AddServiceDefaults("gateway-api");

ConfigureForwardedHeaders(builder.Services, builder.Configuration);

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

ConfigureRedisOutputCache(builder.Services, builder.Configuration);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        builderContext.AddRequestTransform(transformContext =>
        {
            var correlationId = transformContext.HttpContext.Items["CorrelationId"]?.ToString()
                ?? transformContext.HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                transformContext.ProxyRequest.Headers.Remove("X-Correlation-ID");
                transformContext.ProxyRequest.Headers.TryAddWithoutValidation("X-Correlation-ID", correlationId);
            }

            return ValueTask.CompletedTask;
        });
    });

var app = builder.Build();
if (IsForwardedHeadersEnabled(app.Configuration))
{
    app.UseForwardedHeaders();
}

app.UseServiceDefaults();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseOutputCache();
app.MapReverseProxy();
app.Run();

static void ConfigureRedisOutputCache(IServiceCollection services, IConfiguration configuration)
{
    var redisConnectionString = configuration["REDIS_CONNECTIONSTRING"]
        ?? configuration["Redis:ConnectionString"];

    if (string.IsNullOrWhiteSpace(redisConnectionString))
    {
        return;
    }

    services.AddSingleton<IConnectionMultiplexer>(_ =>
    {
        var options = ConfigurationOptions.Parse(redisConnectionString, true);
        options.AbortOnConnectFail = false;
        options.ConnectRetry = 3;
        options.ConnectTimeout = 2000;
        return ConnectionMultiplexer.Connect(options);
    });

    services.Replace(ServiceDescriptor.Singleton<IOutputCacheStore, RedisOutputCacheStore>());
}

static void ConfigureForwardedHeaders(IServiceCollection services, IConfiguration configuration)
{
    if (!IsForwardedHeadersEnabled(configuration))
    {
        return;
    }

    services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

        // Trust all proxies/networks when explicitly enabled. Use only behind trusted reverse proxies.
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

static bool IsForwardedHeadersEnabled(IConfiguration configuration)
    => bool.TryParse(configuration["ForwardedHeaders:Enabled"] ?? configuration["ForwardedHeaders__Enabled"], out var enabled) && enabled;

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
