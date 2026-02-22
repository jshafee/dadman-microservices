using Gateway.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.ContractTests;

public class GatewayOutputCacheRedisRegistrationTests
{

    [Fact]
    public async Task RedisStore_Operations_DoNotThrow_WhenRedisUnavailable()
    {
        using var factory = new GatewayFactoryWithRedis();
        using var scope = factory.Services.CreateScope();

        var store = scope.ServiceProvider.GetRequiredService<IOutputCacheStore>();

        var value = await store.GetAsync("missing", CancellationToken.None);
        await store.SetAsync("key", [1, 2, 3], ["tag"], TimeSpan.FromSeconds(5), CancellationToken.None);
        await store.EvictByTagAsync("tag", CancellationToken.None);

        Assert.Null(value);
    }

    [Fact]
    public void OutputCacheStore_IsRedisBacked_WhenRedisConnectionConfigured()
    {
        using var factory = new GatewayFactoryWithRedis();
        using var scope = factory.Services.CreateScope();

        var store = scope.ServiceProvider.GetRequiredService<IOutputCacheStore>();

        Assert.IsType<RedisOutputCacheStore>(store);
    }

    private sealed class GatewayFactoryWithRedis : WebApplicationFactory<GatewayApiMarker>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Auth:Issuer", TestJwt.Issuer);
            builder.UseSetting("Auth:Audience", TestJwt.Audience);
            builder.UseSetting("Auth:SigningKey", TestJwt.Key);
            builder.UseSetting("REDIS_CONNECTIONSTRING", "localhost:6379,abortConnect=false,connectTimeout=250,syncTimeout=250");

            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ReverseProxy:Clusters:catalog-cluster:Destinations:catalog:Address"] = "http://catalog.test",
                    ["ReverseProxy:Clusters:ordering-cluster:Destinations:ordering:Address"] = "http://ordering.test",
                });
            });
        }
    }
}
