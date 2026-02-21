using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Application;

namespace Ordering.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderingInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("OrderingDb")
            ?? "Server=localhost,1433;Database=OrderingDb;User Id=sa;Password=__SET_VIA_ENV__;TrustServerCertificate=true";

        services.AddDbContext<OrderingDbContext>(options => options.UseSqlServer(connectionString));

        services.AddMassTransit(x =>
        {
            x.AddConsumer<CatalogItemCreatedConsumer>();
            x.AddEntityFrameworkOutbox<OrderingDbContext>(o =>
            {
                o.UseSqlServer();
                o.UseBusOutbox();
            });

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(config["RabbitMq:Host"] ?? "localhost", "/", h =>
                {
                    h.Username(config["RabbitMq:Username"] ?? "admin");
                    h.Password(config["RabbitMq:Password"] ?? "__SET_VIA_ENV__");
                });

                cfg.ReceiveEndpoint("ordering-catalog-item-created", e =>
                {
                    e.ConfigureConsumer<CatalogItemCreatedConsumer>(context);
                    e.UseEntityFrameworkOutbox<OrderingDbContext>(context);
                });
            });
        });

        services.AddScoped<CreateOrderValidator>();
        return services;
    }
}

public class CatalogItemCreatedConsumer : IConsumer<Catalog.Contracts.CatalogItemCreated>
{
    public Task Consume(ConsumeContext<Catalog.Contracts.CatalogItemCreated> context)
    {
        return Task.CompletedTask;
    }
}
