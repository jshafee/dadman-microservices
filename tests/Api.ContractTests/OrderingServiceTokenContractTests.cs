using Xunit;
using Catalog.Api;
using Catalog.Domain;
using Catalog.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Ordering.Api;
using Ordering.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Api.ContractTests;

public class OrderingServiceTokenContractTests
{
    [Fact]
    public async Task PostOrder_WithOrderingWriteUserToken_UsesServiceTokenForCatalogValidation()
    {
        await using var catalogFactory = new CatalogApiFactory();
        _ = catalogFactory.CreateClient();

        var catalogItemId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        await catalogFactory.SeedAsync(new CatalogItem { Id = catalogItemId, Name = "Known Item", Price = 12.34m });

        var serviceToken = TestJwt.Create("catalog.read");
        var capture = new DownstreamRequestCapture();

        await using var orderingFactory = new OrderingApiFactory(catalogFactory, serviceToken, capture);
        var client = orderingFactory.CreateClient();

        var incomingCorrelationId = "corr-service-token-test";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create("ordering.write"));
        client.DefaultRequestHeaders.Add(CorrelationIdPropagationHandler.HeaderName, incomingCorrelationId);

        var payload = $"{{\"catalogItemId\":\"{catalogItemId}\",\"quantity\":1}}";
        var response = await client.PostAsync("/ordering/orders?api-version=1.0", new StringContent(payload, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"Bearer {serviceToken}", capture.AuthorizationHeader);
        Assert.Equal(incomingCorrelationId, capture.CorrelationId);
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

        public async Task SeedAsync(params CatalogItem[] items)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            db.CatalogItems.AddRange(items);
            await db.SaveChangesAsync();
        }
    }

    private sealed class OrderingApiFactory(CatalogApiFactory catalogFactory, string serviceToken, DownstreamRequestCapture capture) : WebApplicationFactory<OrderingApiMarker>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Auth:Issuer", TestJwt.Issuer);
            builder.UseSetting("Auth:Audience", TestJwt.Audience);
            builder.UseSetting("Auth:SigningKey", TestJwt.Key);
            builder.UseSetting("Testing:UseInMemory", "true");
            builder.UseSetting("Services:Catalog:BaseUrl", "http://catalog.test");
            builder.UseSetting("Services:Catalog:ServiceToken", serviceToken);

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ICatalogClient>();
                services.AddHttpClient<ICatalogClient, CatalogClient>((sp, client) =>
                    {
                        var options = sp.GetRequiredService<IOptions<CatalogServiceOptions>>().Value;
                        var resilienceOptions = sp.GetRequiredService<IOptions<CatalogResilienceOptions>>().Value;
                        client.BaseAddress = new Uri(options.BaseUrl);
                        client.Timeout = TimeSpan.FromSeconds(resilienceOptions.TotalTimeoutSeconds);
                    })
                    .ConfigurePrimaryHttpMessageHandler(() => new CaptureHandler(catalogFactory.Server.CreateHandler(), capture))
                    .AddHttpMessageHandler<CorrelationIdPropagationHandler>()
                    .AddHttpMessageHandler<ServiceTokenHandler>()
                    .AddCatalogResilience();
            });
        }
    }

    private sealed class CaptureHandler(HttpMessageHandler innerHandler, DownstreamRequestCapture capture) : DelegatingHandler(innerHandler)
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            capture.AuthorizationHeader = request.Headers.Authorization?.ToString();
            capture.CorrelationId = request.Headers.TryGetValues(CorrelationIdPropagationHandler.HeaderName, out var values)
                ? values.FirstOrDefault()
                : null;

            return base.SendAsync(request, cancellationToken);
        }
    }

    private sealed class DownstreamRequestCapture
    {
        public string? AuthorizationHeader { get; set; }
        public string? CorrelationId { get; set; }
    }
}
