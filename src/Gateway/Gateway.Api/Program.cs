using BuildingBlocks.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServiceDefaults("gateway-api");
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();
app.UseServiceDefaults();
app.MapReverseProxy();
app.Run();
