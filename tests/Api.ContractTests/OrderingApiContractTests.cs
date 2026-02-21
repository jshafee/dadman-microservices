using Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ordering.Api;
using Ordering.Domain;
using Ordering.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Api.ContractTests;

public class OrderingApiContractTests
{
    [Fact]
    public async Task GetOrders_WithoutToken_Returns401()
    {
        await using var factory = new OrderingApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/ordering/orders?api-version=1.0");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_WithWrongScope_Returns403()
    {
        await using var factory = new OrderingApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create("catalog.read"));

        var response = await client.GetAsync("/ordering/orders?api-version=1.0");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_WithOrderingRead_Returns200()
    {
        await using var factory = new OrderingApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create("ordering.read"));

        var response = await client.GetAsync("/ordering/orders?api-version=1.0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostOrders_WithOrderingWrite_IsAuthorized()
    {
        await using var factory = new OrderingApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create("ordering.write"));

        var payload = "{\"catalogItemId\":\"11111111-1111-1111-1111-111111111111\",\"quantity\":1}";
        var response = await client.PostAsync("/ordering/orders?api-version=1.0", new StringContent(payload, Encoding.UTF8, "application/json"));

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiV1_ReturnsOrderingPath()
    {
        await using var factory = new OrderingApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/ordering/orders", body);
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

    private sealed class FakeCatalogClient : ICatalogClient
    {
        public Task<bool> CatalogItemExistsAsync(Guid catalogItemId, CancellationToken cancellationToken)
            => Task.FromResult(true);
    }
}
