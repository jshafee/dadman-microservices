using BuildingBlocks.Validation;
using BuildingBlocks.ServiceDefaults;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Ordering.Application;
using Ordering.Contracts;
using Ordering.Domain;
using Ordering.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServiceDefaults("ordering-api");
builder.Services.AddOrderingInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseServiceDefaults();

app.MapGet("/ordering/orders", async (OrderingDbContext db) =>
    await db.Orders.AsNoTracking().ToListAsync());

app.MapPost("/ordering/orders", async (
    CreateOrderCommand cmd,
    CreateOrderValidator validator,
    OrderingDbContext db,
    IPublishEndpoint publisher,
    CancellationToken ct) =>
{
    var result = await validator.ValidateAsync(cmd, ct);
    if (!result.IsValid)
    {
        return Results.ValidationProblem(result.ToValidationProblems());
    }

    var order = new Order { CatalogItemId = cmd.CatalogItemId, Quantity = cmd.Quantity };
    db.Orders.Add(order);
    await publisher.Publish(new OrderSubmitted(order.Id, order.CatalogItemId, order.Quantity), ct);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/ordering/orders/{order.Id}", order);
});

app.MapPost("/ordering/migrate", async (OrderingDbContext db, CancellationToken ct) =>
{
    await db.Database.MigrateAsync(ct);
    return Results.Ok();
});

app.Run();
