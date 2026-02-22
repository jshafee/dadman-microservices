using Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Ordering.Infrastructure;
using Polly.Timeout;
using System.Net;

namespace Api.ContractTests;

public class OrderingCatalogClientResilienceTests
{
    [Fact]
    public async Task CatalogClient_RetriesTransientFailures_ThenSucceeds()
    {
        var handler = new FailThenSuccessHandler(failuresBeforeSuccess: 2);
        await using var provider = BuildServiceProvider(handler, new CatalogResilienceOptions
        {
            TotalTimeoutSeconds = 5,
            RetryCount = 2,
            RetryBaseDelayMs = 1,
            CircuitBreakerFailureRatio = 1,
            CircuitBreakerSamplingDurationSeconds = 30,
            CircuitBreakerMinimumThroughput = 100,
            CircuitBreakerBreakDurationSeconds = 30
        });

        var client = provider.GetRequiredService<ICatalogClient>();

        var exists = await client.CatalogItemExistsAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(exists);
        Assert.Equal(3, handler.AttemptCount);
    }

    [Fact]
    public async Task CatalogClient_LongRunningRequest_TriggersTimeout()
    {
        var handler = new SlowHandler(TimeSpan.FromSeconds(5));
        await using var provider = BuildServiceProvider(handler, new CatalogResilienceOptions
        {
            TotalTimeoutSeconds = 1,
            RetryCount = 1,
            RetryBaseDelayMs = 1,
            CircuitBreakerFailureRatio = 1,
            CircuitBreakerSamplingDurationSeconds = 30,
            CircuitBreakerMinimumThroughput = 100,
            CircuitBreakerBreakDurationSeconds = 30
        });

        var client = provider.GetRequiredService<ICatalogClient>();

        var ex = await Assert.ThrowsAnyAsync<Exception>(() => client.CatalogItemExistsAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.True(ex is TimeoutRejectedException or TaskCanceledException or OperationCanceledException);
        Assert.True(handler.AttemptCount >= 1);
    }

    private static ServiceProvider BuildServiceProvider(HttpMessageHandler handler, CatalogResilienceOptions resilienceOptions)
    {
        var services = new ServiceCollection();

        services.AddHttpContextAccessor();
        services.AddTransient<CorrelationIdPropagationHandler>();
        services.AddTransient<ServiceTokenHandler>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Services:Catalog:BaseUrl"] = "http://catalog.test",
                ["Services:Catalog:ServiceToken"] = TestJwt.Create("catalog.read"),
                ["Services:Catalog:Resilience:TotalTimeoutSeconds"] = resilienceOptions.TotalTimeoutSeconds.ToString(),
                ["Services:Catalog:Resilience:RetryCount"] = resilienceOptions.RetryCount.ToString(),
                ["Services:Catalog:Resilience:RetryBaseDelayMs"] = resilienceOptions.RetryBaseDelayMs.ToString(),
                ["Services:Catalog:Resilience:CircuitBreakerFailureRatio"] = resilienceOptions.CircuitBreakerFailureRatio.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["Services:Catalog:Resilience:CircuitBreakerSamplingDurationSeconds"] = resilienceOptions.CircuitBreakerSamplingDurationSeconds.ToString(),
                ["Services:Catalog:Resilience:CircuitBreakerMinimumThroughput"] = resilienceOptions.CircuitBreakerMinimumThroughput.ToString(),
                ["Services:Catalog:Resilience:CircuitBreakerBreakDurationSeconds"] = resilienceOptions.CircuitBreakerBreakDurationSeconds.ToString()
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<CatalogServiceOptions>(configuration.GetSection(CatalogServiceOptions.SectionName));
        services.Configure<CatalogResilienceOptions>(configuration.GetSection(CatalogResilienceOptions.SectionName));
        services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment());

        services.AddHttpClient<ICatalogClient, CatalogClient>((sp, client) =>
            {
                var catalogOptions = sp.GetRequiredService<IOptions<CatalogServiceOptions>>().Value;
                var configuredResilience = sp.GetRequiredService<IOptions<CatalogResilienceOptions>>().Value;
                client.BaseAddress = new Uri(catalogOptions.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(configuredResilience.TotalTimeoutSeconds);
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddHttpMessageHandler<CorrelationIdPropagationHandler>()
            .AddHttpMessageHandler<ServiceTokenHandler>()
            .AddCatalogResilience();

        return services.BuildServiceProvider();
    }

    private sealed class FailThenSuccessHandler(int failuresBeforeSuccess) : HttpMessageHandler
    {
        private int _attemptCount;
        public int AttemptCount => _attemptCount;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var attempt = Interlocked.Increment(ref _attemptCount);
            if (attempt <= failuresBeforeSuccess)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class SlowHandler(TimeSpan delay) : HttpMessageHandler
    {
        private int _attemptCount;
        public int AttemptCount => _attemptCount;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _attemptCount);
            await Task.Delay(delay, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Api.ContractTests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
