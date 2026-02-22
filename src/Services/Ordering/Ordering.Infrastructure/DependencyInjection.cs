using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Microsoft.Extensions.Options;
using Ordering.Application;

namespace Ordering.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderingInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var useInMemory = bool.TryParse(config["Testing:UseInMemory"], out var parsedUseInMemory) && parsedUseInMemory;
        var connectionString = config.GetConnectionString("OrderingDb")
            ?? "Server=localhost,1433;Database=OrderingDb;User Id=sa;Password=__SET_VIA_ENV__;TrustServerCertificate=true";

        services.AddDbContext<OrderingDbContext>(options =>
        {
            if (useInMemory)
            {
                options.UseInMemoryDatabase("OrderingDb");
                return;
            }

            options.UseSqlServer(connectionString, sqlServerOptions => sqlServerOptions.EnableRetryOnFailure());
        });

        services.Configure<CatalogServiceOptions>(config.GetSection(CatalogServiceOptions.SectionName));
        services.Configure<CatalogResilienceOptions>(config.GetSection(CatalogResilienceOptions.SectionName));
        services.AddHttpContextAccessor();
        services.AddTransient<CorrelationIdPropagationHandler>();
        services.AddTransient<ServiceTokenHandler>();
        services.AddHttpClient<ICatalogClient, CatalogClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<CatalogServiceOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
            })
            .AddHttpMessageHandler<CorrelationIdPropagationHandler>()
            .AddHttpMessageHandler<ServiceTokenHandler>()
            .AddCatalogResilience();


        if (!useInMemory)
        {
            var rabbitMqHost = config["RabbitMq:Host"] ?? "localhost";
            var rabbitMqUsername = config["RabbitMq:Username"] ?? "admin";
            var rabbitMqPassword = config["RabbitMq:Password"] ?? "__SET_VIA_ENV__";
            var rabbitMqConnectionString = $"amqp://{Uri.EscapeDataString(rabbitMqUsername)}:{Uri.EscapeDataString(rabbitMqPassword)}@{rabbitMqHost}:5672";

            services.AddHealthChecks()
                .AddSqlServer(
                    connectionString: connectionString,
                    name: "sqlserver",
                    tags: ["ready"])
                .AddRabbitMQ(
                    _ =>
                    {
                        var factory = new ConnectionFactory
                        {
                            Uri = new Uri(rabbitMqConnectionString)
                        };

                        return factory.CreateConnectionAsync().GetAwaiter().GetResult();
                    },
                    name: "rabbitmq",
                    tags: ["ready"]);
        }

        services.AddMassTransit(x =>
        {
            x.AddConsumer<CatalogItemCreatedConsumer>();

            if (useInMemory)
            {
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ReceiveEndpoint("ordering-catalog-item-created", e =>
                    {
                        e.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)));
                        e.ConfigureConsumer<CatalogItemCreatedConsumer>(context);
                    });
                });

                return;
            }

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
                    e.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)));
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
