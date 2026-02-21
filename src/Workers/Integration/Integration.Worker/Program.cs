using Integration.Worker;
using BuildingBlocks.ServiceDefaults;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddServiceDefaults("integration-worker");
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"] ?? "admin");
            h.Password(builder.Configuration["RabbitMq:Password"] ?? "__SET_VIA_ENV__");
        });
    });
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
