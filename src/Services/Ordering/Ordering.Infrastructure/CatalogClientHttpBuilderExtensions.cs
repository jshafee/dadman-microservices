using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace Ordering.Infrastructure;

public static class CatalogClientHttpBuilderExtensions
{
    public static IHttpClientBuilder AddCatalogResilience(this IHttpClientBuilder builder)
    {
        builder.AddResilienceHandler("catalog-resilience", static (pipeline, context) =>
        {
            var options = context.ServiceProvider.GetRequiredService<IOptions<CatalogResilienceOptions>>().Value;

            pipeline.AddTimeout(TimeSpan.FromSeconds(options.TotalTimeoutSeconds));
            pipeline.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = Math.Max(1, options.RetryCount),
                Delay = TimeSpan.FromMilliseconds(options.RetryBaseDelayMs),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = false
            });
            pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = options.CircuitBreakerFailureRatio,
                SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreakerSamplingDurationSeconds),
                MinimumThroughput = options.CircuitBreakerMinimumThroughput,
                BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakerBreakDurationSeconds)
            });
        });

        return builder;
    }
}
