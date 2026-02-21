using BuildingBlocks.Validation;
using BuildingBlocks.ServiceDefaults;
using Catalog.Application;
using Catalog.Contracts;
using Catalog.Domain;
using Catalog.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServiceDefaults("catalog-api");
builder.Services.AddCatalogInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseServiceDefaults();

app.MapGet("/catalog/items", async (CatalogDbContext db) =>
    await db.CatalogItems.AsNoTracking().ToListAsync());

app.MapPost("/catalog/items", async (
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
});

app.MapPost("/catalog/migrate", async (CatalogDbContext db, CancellationToken ct) =>
{
    await db.Database.MigrateAsync(ct);
    return Results.Ok();
});

app.Run();
