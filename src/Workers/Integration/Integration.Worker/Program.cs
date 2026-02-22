using BuildingBlocks.ServiceDefaults;
using Integration.Worker;
using MassTransit;
using Serilog;
using Serilog.Enrichers.Span;

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((context, _, loggerConfiguration) =>
    {
        var seqServerUrl = context.Configuration["Seq:ServerUrl"];
        var seqApiKey = context.Configuration["Seq:ApiKey"];

        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithSpan()
            .Enrich.WithProperty("service.name", "integration-worker")
            .WriteTo.Console();

        if (!string.IsNullOrWhiteSpace(seqServerUrl))
        {
            loggerConfiguration.WriteTo.Seq(seqServerUrl, apiKey: seqApiKey);
        }
    })
    .ConfigureServices((context, services) =>
    {
        services.AddServiceDefaults("integration-worker");
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((_, cfg) =>
            {
                cfg.Host(context.Configuration["RabbitMq:Host"] ?? "localhost", "/", h =>
                {
                    h.Username(context.Configuration["RabbitMq:Username"] ?? "admin");
                    h.Password(context.Configuration["RabbitMq:Password"] ?? "__SET_VIA_ENV__");
                });
            });
        });

        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
