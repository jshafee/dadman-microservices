using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
                var resilienceOptions = sp.GetRequiredService<IOptions<CatalogResilienceOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(resilienceOptions.TotalTimeoutSeconds);
            })
            .AddHttpMessageHandler<CorrelationIdPropagationHandler>()
            .AddHttpMessageHandler<ServiceTokenHandler>()
            .AddCatalogResilience();

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
