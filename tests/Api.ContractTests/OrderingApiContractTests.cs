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
using System.Text.Json;

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
    public async Task GetOrders_WithoutApiVersion_UsesDefaultV1()
    {
        await using var factory = new OrderingApiFactory();
        await factory.SeedAsync(new Order
        {
            CatalogItemId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Quantity = 2
        });
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create("ordering.read"));

        var defaultResponse = await client.GetAsync("/ordering/orders");
        var v1Response = await client.GetAsync("/ordering/orders?api-version=1.0");

        Assert.Equal(HttpStatusCode.OK, defaultResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, v1Response.StatusCode);

        using var defaultDoc = JsonDocument.Parse(await defaultResponse.Content.ReadAsStringAsync());
        using var v1Doc = JsonDocument.Parse(await v1Response.Content.ReadAsStringAsync());

        var defaultOrder = defaultDoc.RootElement[0];
        var v1Order = v1Doc.RootElement[0];

        Assert.False(defaultOrder.TryGetProperty("summary", out _));
        Assert.False(v1Order.TryGetProperty("summary", out _));
        Assert.Equal(v1Order.GetProperty("quantity").GetInt32(), defaultOrder.GetProperty("quantity").GetInt32());
        Assert.Equal(v1Order.GetProperty("catalogItemId").GetGuid(), defaultOrder.GetProperty("catalogItemId").GetGuid());
    }

    [Fact]
    public async Task GetOrders_WithUnsupportedApiVersion_ReturnsClientError()
    {
        await using var factory = new OrderingApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create("ordering.read"));

        var response = await client.GetAsync("/ordering/orders?api-version=9.9");

        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.NotFound });
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
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

        public async Task SeedAsync(params Order[] orders)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
            db.Orders.AddRange(orders);
            await db.SaveChangesAsync();
        }
    }

    private sealed class FakeCatalogClient : ICatalogClient
    {
        public Task<bool> CatalogItemExistsAsync(Guid catalogItemId, CancellationToken cancellationToken)
            => Task.FromResult(true);
    }
}
