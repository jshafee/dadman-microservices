using Catalog.Application;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("CatalogDb")
            ?? "Server=localhost,1433;Database=CatalogDb;User Id=sa;Password=__SET_VIA_ENV__;TrustServerCertificate=true";

        services.AddDbContext<CatalogDbContext>(options => options.UseSqlServer(connectionString));

        services.AddMassTransit(x =>
        {
            x.AddConsumer<OrderSubmittedConsumer>();
            x.AddEntityFrameworkOutbox<CatalogDbContext>(o =>
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

                cfg.ReceiveEndpoint("catalog-order-submitted", e =>
                {
                    e.ConfigureConsumer<OrderSubmittedConsumer>(context);
                    e.UseEntityFrameworkOutbox<CatalogDbContext>(context);
                });
            });
        });

        services.AddScoped<CreateCatalogItemValidator>();
        return services;
    }
}

public class OrderSubmittedConsumer : IConsumer<Ordering.Contracts.OrderSubmitted>
{
    public Task Consume(ConsumeContext<Ordering.Contracts.OrderSubmitted> context)
    {
        return Task.CompletedTask;
    }
}
