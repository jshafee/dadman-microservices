using Xunit;
using Catalog.Domain;
using Catalog.Infrastructure;
using Catalog.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Api.ContractTests;

public class CatalogApiContractTests
{
    [Fact]
    public async Task GetItems_WithoutToken_Returns401()
    {
        await using var factory = new CatalogApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/catalog/items?api-version=1.0");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetItems_WithTokenWithoutCatalogRead_Returns403()
    {
        await using var factory = new CatalogApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create("ordering.read"));

        var response = await client.GetAsync("/catalog/items?api-version=1.0");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetItems_WithCatalogRead_Returns200()
    {
        await using var factory = new CatalogApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create("catalog.read"));

        var response = await client.GetAsync("/catalog/items?api-version=1.0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiV1_ReturnsCatalogPath()
    {
        await using var factory = new CatalogApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/catalog/items", body);
    }

    [Fact]
    public async Task VersionedGetItems_V1AndV2_Return200_AndV2Differs()
    {
        await using var factory = new CatalogApiFactory();
        await factory.SeedAsync(new CatalogItem { Name = "Widget", Price = 10.50m });
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create("catalog.read"));

        var v1Response = await client.GetAsync("/catalog/items?api-version=1.0");
        var v2Response = await client.GetAsync("/catalog/items?api-version=2.0");

        Assert.Equal(HttpStatusCode.OK, v1Response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, v2Response.StatusCode);

        using var v1Doc = JsonDocument.Parse(await v1Response.Content.ReadAsStringAsync());
        using var v2Doc = JsonDocument.Parse(await v2Response.Content.ReadAsStringAsync());

        var v1Item = v1Doc.RootElement[0];
        var v2Item = v2Doc.RootElement[0];

        Assert.False(v1Item.TryGetProperty("displayName", out _));
        Assert.True(v2Item.TryGetProperty("displayName", out _));
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
}
