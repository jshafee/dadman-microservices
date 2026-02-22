using Asp.Versioning;
using Asp.Versioning.Builder;
using BuildingBlocks.Security;
using BuildingBlocks.ServiceDefaults;
using BuildingBlocks.Validation;
using MassTransit;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Enrichers.Span;
using Ordering.Application;
using Ordering.Contracts;
using Ordering.Domain;
using Ordering.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);

builder.Host.UseSerilog((context, _, loggerConfiguration) =>
{
    var seqServerUrl = context.Configuration["Seq:ServerUrl"];
    var seqApiKey = context.Configuration["Seq:ApiKey"];

    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithSpan()
        .Enrich.WithProperty("service.name", "ordering-api")
        .WriteTo.Console();

    if (!string.IsNullOrWhiteSpace(seqServerUrl))
    {
        loggerConfiguration.WriteTo.Seq(seqServerUrl, apiKey: seqApiKey);
    }
});

builder.Services.AddServiceDefaults("ordering-api");
builder.Services.AddJwtSecurity(builder.Configuration);
builder.Services.AddOrderingInfrastructure(builder.Configuration);
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

var orderingRead = app.MapGroup("/ordering")
    .WithApiVersionSet(versionSet)
    .RequireOrderingReadScope();

var orderingWrite = app.MapGroup("/ordering")
    .WithApiVersionSet(versionSet)
    .RequireOrderingWriteScope();

orderingRead.MapGet("/orders", async (OrderingDbContext db) =>
        await db.Orders.AsNoTracking().ToListAsync())
    .MapToApiVersion(new ApiVersion(1, 0));

orderingRead.MapGet("/orders", async (OrderingDbContext db) =>
        await db.Orders.AsNoTracking()
            .Select(order => new
            {
                order.Id,
                order.CatalogItemId,
                order.Quantity,
                Summary = $"Item {order.CatalogItemId} x {order.Quantity}"
            })
            .ToListAsync())
    .MapToApiVersion(new ApiVersion(2, 0));

orderingWrite.MapPost("/orders", async (
        CreateOrderCommand cmd,
        CreateOrderValidator validator,
        OrderingDbContext db,
        ICatalogClient catalogClient,
        IPublishEndpoint publisher,
        CancellationToken ct) =>
    {
        var result = await validator.ValidateAsync(cmd, ct);
        if (!result.IsValid)
        {
            return Results.ValidationProblem(result.ToValidationProblems());
        }

        var catalogItemExists = await catalogClient.CatalogItemExistsAsync(cmd.CatalogItemId, ct);
        if (!catalogItemExists)
        {
            return Results.NotFound(new { message = "Catalog item was not found.", cmd.CatalogItemId });
        }

        var order = new Order { CatalogItemId = cmd.CatalogItemId, Quantity = cmd.Quantity };
        db.Orders.Add(order);
        await publisher.Publish(new OrderSubmitted(order.Id, order.CatalogItemId, order.Quantity), ct);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/ordering/orders/{order.Id}", order);
    })
    .MapToApiVersion(new ApiVersion(1, 0));

if (app.Environment.IsDevelopment())
{
    orderingWrite.MapPost("/migrate", async (OrderingDbContext db, CancellationToken ct) =>
        {
            await db.Database.MigrateAsync(ct);
            return Results.Ok();
        })
        .MapToApiVersion(new ApiVersion(1, 0));
}

app.Run();

public partial class Program { }

namespace Ordering.Api
{
    public sealed class OrderingApiMarker { }
}
