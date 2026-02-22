using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace BuildingBlocks.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    public static IServiceCollection AddServiceDefaults(this IServiceCollection services, string? serviceName = null)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live", "ready"]);

        // Intentionally configure OpenTelemetry for traces + metrics only.
        // Logging is handled via Serilog/Seq in service entrypoints to avoid duplicate log pipelines.
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName ?? "dadman-service"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource("MassTransit")
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter());

        return services;
    }

    public static WebApplication UseServiceDefaults(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
            context.Request.Headers["X-Correlation-ID"] = correlationId;
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-Correlation-ID"] = correlationId;
                return Task.CompletedTask;
            });
            context.Items["CorrelationId"] = correlationId;

            var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
            var scopedLogger = loggerFactory.CreateLogger("CorrelationScope");
            using (scopedLogger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = correlationId }))
            {
                await next();
            }
        });

        app.Use(async (context, next) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("RequestLogging");
            var started = DateTimeOffset.UtcNow;
            await next();
            logger.LogInformation("{Method} {Path} -> {StatusCode} in {ElapsedMs}ms CorrelationId={CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                (DateTimeOffset.UtcNow - started).TotalMilliseconds,
                context.Items["CorrelationId"]);
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        }).AllowAnonymous();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        }).AllowAnonymous();

        return app;
    }
}
