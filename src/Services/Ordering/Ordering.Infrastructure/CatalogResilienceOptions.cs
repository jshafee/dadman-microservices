namespace Ordering.Infrastructure;

public sealed class CatalogResilienceOptions
{
    public const string SectionName = "Services:Catalog:Resilience";

    public int TotalTimeoutSeconds { get; set; } = 10;
    public int RetryCount { get; set; } = 3;
    public int RetryBaseDelayMs { get; set; } = 200;
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;
    public int CircuitBreakerSamplingDurationSeconds { get; set; } = 30;
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;
    public int CircuitBreakerBreakDurationSeconds { get; set; } = 15;
}
