using Asp.Versioning;
using Asp.Versioning.Builder;
using BuildingBlocks.Security;
using BuildingBlocks.ServiceDefaults;
using BuildingBlocks.Validation;
using Catalog.Application;
using Catalog.Contracts;
using Catalog.Domain;
using Catalog.Infrastructure;
using MassTransit;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, loggerConfiguration) =>
{
    var seqServerUrl = context.Configuration["Seq:ServerUrl"];
    var seqApiKey = context.Configuration["Seq:ApiKey"];

    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProperty("service.name", "catalog-api")
        .WriteTo.Console();

    if (!string.IsNullOrWhiteSpace(seqServerUrl))
    {
        loggerConfiguration.WriteTo.Seq(seqServerUrl, apiKey: seqApiKey);
    }
});

builder.Services.AddServiceDefaults("catalog-api");
builder.Services.AddJwtSecurity(builder.Configuration);
builder.Services.AddCatalogInfrastructure(builder.Configuration);
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new QueryStringApiVersionReader("api-version");
});
builder.Services.AddOpenApi("v1", options =>
{
    options.ShouldInclude = description => description.ActionDescriptor.EndpointMetadata
        .OfType<ApiVersionMetadata>()
        .Any(metadata => metadata.Map(ApiVersionMapping.Explicit | ApiVersionMapping.Implicit).DeclaredApiVersions
            .Contains(new ApiVersion(1, 0)));
});
builder.Services.AddOpenApi("v2", options =>
{
    options.ShouldInclude = description => description.ActionDescriptor.EndpointMetadata
        .OfType<ApiVersionMetadata>()
        .Any(metadata => metadata.Map(ApiVersionMapping.Explicit | ApiVersionMapping.Implicit).DeclaredApiVersions
            .Contains(new ApiVersion(2, 0)));
});

var app = builder.Build();
app.UseServiceDefaults();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/openapi/{documentName}.json").AllowAnonymous();
}

var versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .HasApiVersion(new ApiVersion(2, 0))
    .ReportApiVersions()
    .Build();

var catalogRead = app.MapGroup("/catalog")
    .WithApiVersionSet(versionSet)
    .RequireCatalogReadScope();

var catalogWrite = app.MapGroup("/catalog")
    .WithApiVersionSet(versionSet)
    .RequireCatalogWriteScope();

catalogRead.MapGet("/items", async (CatalogDbContext db) =>
        await db.CatalogItems.AsNoTracking().ToListAsync())
    .MapToApiVersion(new ApiVersion(1, 0));

catalogRead.MapGet("/items", async (CatalogDbContext db) =>
        await db.CatalogItems.AsNoTracking()
            .Select(item => new
            {
                item.Id,
                item.Name,
                item.Price,
                DisplayName = $"{item.Name} (${item.Price:0.00})"
            })
            .ToListAsync())
    .MapToApiVersion(new ApiVersion(2, 0));


catalogRead.MapGet("/items/{id:guid}", async (Guid id, CatalogDbContext db, CancellationToken ct) =>
    {
        var item = await db.CatalogItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? Results.NotFound() : Results.Ok(item);
    })
    .MapToApiVersion(new ApiVersion(1, 0));

catalogRead.MapGet("/items/{id:guid}", async (Guid id, CatalogDbContext db, CancellationToken ct) =>
    {
        var item = await db.CatalogItems.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Price,
                DisplayName = $"{x.Name} (${x.Price:0.00})"
            })
            .FirstOrDefaultAsync(ct);

        return item is null ? Results.NotFound() : Results.Ok(item);
    })
    .MapToApiVersion(new ApiVersion(2, 0));

catalogWrite.MapPost("/items", async (
        CreateCatalogItemCommand cmd,
        CreateCatalogItemValidator validator,
        CatalogDbContext db,
        IPublishEndpoint publisher,
        CancellationToken ct) =>
    {
        var result = await validator.ValidateAsync(cmd, ct);
        if (!result.IsValid)
        {
            return Results.ValidationProblem(result.ToValidationProblems());
        }

        var item = new CatalogItem { Name = cmd.Name, Price = cmd.Price };
        db.CatalogItems.Add(item);
        await publisher.Publish(new CatalogItemCreated(item.Id, item.Name, item.Price), ct);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/catalog/items/{item.Id}", item);
    })
    .MapToApiVersion(new ApiVersion(1, 0));

if (app.Environment.IsDevelopment())
{
    catalogWrite.MapPost("/migrate", async (CatalogDbContext db, CancellationToken ct) =>
        {
            await db.Database.MigrateAsync(ct);
            return Results.Ok();
        })
        .MapToApiVersion(new ApiVersion(1, 0));
}

app.Run();

public partial class Program { }

namespace Catalog.Api
{
    public sealed class CatalogApiMarker { }
}
