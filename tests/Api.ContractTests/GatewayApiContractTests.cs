using Xunit;
using Catalog.Api;
using Gateway.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ordering.Api;
using Ordering.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Yarp.ReverseProxy.Forwarder;

namespace Api.ContractTests;

public class GatewayApiContractTests
{
    [Fact]
    public async Task Requests_WithoutToken_Return401()
    {
        await using var host = GatewayTestHost.Create();

        var catalogResponse = await host.Client.GetAsync("/catalog/items?api-version=1.0");
        var orderingResponse = await host.Client.GetAsync("/ordering/orders?api-version=1.0");

        Assert.Equal(HttpStatusCode.Unauthorized, catalogResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, orderingResponse.StatusCode);
    }

    [Fact]
    public async Task Requests_WithMissingScope_Return403()
    {
        await using var host = GatewayTestHost.Create();

        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create("ordering.read"));
        var catalogResponse = await host.Client.GetAsync("/catalog/items?api-version=1.0");

        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create("catalog.read"));
        var orderingResponse = await host.Client.GetAsync("/ordering/orders?api-version=1.0");

        Assert.Equal(HttpStatusCode.Forbidden, catalogResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, orderingResponse.StatusCode);
    }

    [Fact]
    public async Task Requests_WithCorrectScopes_ReturnSuccess()
    {
        await using var host = GatewayTestHost.Create();

        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create("catalog.read"));
        var catalogGet = await host.Client.GetAsync("/catalog/items?api-version=1.0");

        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create("ordering.read"));
        var orderingGet = await host.Client.GetAsync("/ordering/orders?api-version=1.0");

        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create("catalog.write"));
        var payload = "{\"name\":\"Gateway Item\",\"price\":9.99}";
        var catalogPost = await host.Client.PostAsync("/catalog/items?api-version=1.0", new StringContent(payload, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, catalogGet.StatusCode);
        Assert.Equal(HttpStatusCode.OK, orderingGet.StatusCode);
        Assert.Equal(HttpStatusCode.Created, catalogPost.StatusCode);
    }

    [Fact]
    public async Task Requests_ExceedingWriteRateLimit_Return429()
    {
        await using var host = GatewayTestHost.Create();

        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create("catalog.write"));

        var requests = Enumerable.Range(0, 40).Select(index =>
        {
            var payload = $"{{\"name\":\"Item-{index}\",\"price\":1.00}}";
            return host.Client.PostAsync("/catalog/items?api-version=1.0", new StringContent(payload, Encoding.UTF8, "application/json"));
        });

        var responses = await Task.WhenAll(requests);

        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task CatalogRead_WithAuthorizedUser_IsServedFromGatewayCache()
    {
        await using var host = GatewayTestHost.Create();
        var before = host.CatalogReadForwardCount;

        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create("catalog.read"));

        var first = await host.Client.GetAsync("/catalog/items?api-version=1.0");
        var second = await host.Client.GetAsync("/catalog/items?api-version=1.0");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(before + 1, host.CatalogReadForwardCount);
    }

    [Fact]
    public async Task CatalogRead_FromCache_UsesCurrentRequestCorrelationId()
    {
        await using var host = GatewayTestHost.Create();

        host.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create("catalog.read"));

        using var firstRequest = new HttpRequestMessage(HttpMethod.Get, "/catalog/items?api-version=1.0");
        firstRequest.Headers.Add("X-Correlation-ID", "corr-1");
        var first = await host.Client.SendAsync(firstRequest);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal("corr-1", first.Headers.GetValues("X-Correlation-ID").Single());
        Assert.Equal("corr-1", host.LastCatalogReadForwardCorrelationId);

        using var secondRequest = new HttpRequestMessage(HttpMethod.Get, "/catalog/items?api-version=1.0");
        secondRequest.Headers.Add("X-Correlation-ID", "corr-2");
        var second = await host.Client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal("corr-2", second.Headers.GetValues("X-Correlation-ID").Single());
        Assert.Equal(1, host.CatalogReadForwardCount);
    }

    [Fact]
    public async Task UnauthorizedResponses_AreNotCached()
    {
        await using var host = GatewayTestHost.Create();

        var first = await host.Client.GetAsync("/catalog/items?api-version=1.0");
        var second = await host.Client.GetAsync("/catalog/items?api-version=1.0");

        Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
        Assert.Equal(0, host.CatalogReadForwardCount);
    }

    private sealed class GatewayTestHost : IAsyncDisposable
    {
        private readonly CatalogApiFactory _catalogFactory;
        private readonly OrderingApiFactory _orderingFactory;
        private readonly GatewayApiFactory _gatewayFactory;

        private GatewayTestHost(CatalogApiFactory catalogFactory, OrderingApiFactory orderingFactory, GatewayApiFactory gatewayFactory, HttpClient client)
        {
            _catalogFactory = catalogFactory;
            _orderingFactory = orderingFactory;
            _gatewayFactory = gatewayFactory;
            Client = client;
        }

        public HttpClient Client { get; }
        public int CatalogReadForwardCount => _gatewayFactory.CatalogReadForwardCount;
        public string? LastCatalogReadForwardCorrelationId => _gatewayFactory.LastCatalogReadForwardCorrelationId;

        public static GatewayTestHost Create()
        {
            var catalogFactory = new CatalogApiFactory();
            _ = catalogFactory.CreateClient();

            var orderingFactory = new OrderingApiFactory();
            _ = orderingFactory.CreateClient();

            var gatewayFactory = new GatewayApiFactory(catalogFactory, orderingFactory);
            var gatewayClient = gatewayFactory.CreateClient();

            return new GatewayTestHost(catalogFactory, orderingFactory, gatewayFactory, gatewayClient);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _gatewayFactory.DisposeAsync();
            await _orderingFactory.DisposeAsync();
            await _catalogFactory.DisposeAsync();
        }
    }

    private sealed class GatewayApiFactory(CatalogApiFactory catalogFactory, OrderingApiFactory orderingFactory) : WebApplicationFactory<GatewayApiMarker>
    {
        private readonly TestForwarderHttpClientFactory _forwarderFactory = new(catalogFactory, orderingFactory);

        public int CatalogReadForwardCount => _forwarderFactory.CatalogReadForwardCount;
        public string? LastCatalogReadForwardCorrelationId => _forwarderFactory.LastCatalogReadForwardCorrelationId;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Auth:Issuer", TestJwt.Issuer);
            builder.UseSetting("Auth:Audience", TestJwt.Audience);
            builder.UseSetting("Auth:SigningKey", TestJwt.Key);
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ReverseProxy:Clusters:catalog-cluster:Destinations:catalog:Address"] = "http://catalog.test",
                    ["ReverseProxy:Clusters:ordering-cluster:Destinations:ordering:Address"] = "http://ordering.test"
                });
            });
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IForwarderHttpClientFactory>();
                services.AddSingleton<IForwarderHttpClientFactory>(_ => _forwarderFactory);
            });
        }
    }

    private sealed class CatalogApiFactory : WebApplicationFactory<CatalogApiMarker>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Auth:Issuer", TestJwt.Issuer);
            builder.UseSetting("Auth:Audience", TestJwt.Audience);
            builder.UseSetting("Auth:SigningKey", TestJwt.Key);
            builder.UseSetting("Testing:UseInMemory", "true");
        }
    }

    private sealed class OrderingApiFactory : WebApplicationFactory<OrderingApiMarker>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Auth:Issuer", TestJwt.Issuer);
            builder.UseSetting("Auth:Audience", TestJwt.Audience);
            builder.UseSetting("Auth:SigningKey", TestJwt.Key);
            builder.UseSetting("Testing:UseInMemory", "true");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ICatalogClient>();
                services.AddSingleton<ICatalogClient, FakeCatalogClient>();
            });
        }
    }

    private sealed class TestForwarderHttpClientFactory : IForwarderHttpClientFactory
    {
        private readonly HttpMessageInvoker _catalogInvoker;
        private readonly HttpMessageInvoker _orderingInvoker;
        private int _catalogReadForwardCount;
        private string? _lastCatalogReadForwardCorrelationId;

        public TestForwarderHttpClientFactory(CatalogApiFactory catalogFactory, OrderingApiFactory orderingFactory)
        {
            _catalogInvoker = new HttpMessageInvoker(new CountingHandler(
                catalogFactory.Server.CreateHandler(),
                () => Interlocked.Increment(ref _catalogReadForwardCount),
                correlationId => Volatile.Write(ref _lastCatalogReadForwardCorrelationId, correlationId)));
            _orderingInvoker = new HttpMessageInvoker(orderingFactory.Server.CreateHandler());
        }

        public int CatalogReadForwardCount => Volatile.Read(ref _catalogReadForwardCount);
        public string? LastCatalogReadForwardCorrelationId => Volatile.Read(ref _lastCatalogReadForwardCorrelationId);

        public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context)
        {
            return context.ClusterId switch
            {
                "catalog-cluster" => _catalogInvoker,
                "ordering-cluster" => _orderingInvoker,
                _ => new HttpMessageInvoker(new HttpClientHandler())
            };
        }
    }

    private sealed class CountingHandler(
        HttpMessageHandler innerHandler,
        Action onCatalogReadForward,
        Action<string?> onCatalogReadForwardCorrelationId) : DelegatingHandler(innerHandler)
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get
                && request.RequestUri is not null
                && request.RequestUri.AbsolutePath.StartsWith("/catalog/items", StringComparison.OrdinalIgnoreCase)
                && request.RequestUri.Query.Contains("api-version=1.0", StringComparison.Ordinal))
            {
                onCatalogReadForward();
                request.Headers.TryGetValues("X-Correlation-ID", out var correlationHeaderValues);
                onCatalogReadForwardCorrelationId(correlationHeaderValues?.SingleOrDefault());
            }

            return base.SendAsync(request, cancellationToken);
        }
    }

    private sealed class FakeCatalogClient : ICatalogClient
    {
        public Task<bool> CatalogItemExistsAsync(Guid catalogItemId, CancellationToken cancellationToken)
            => Task.FromResult(true);
    }
}
