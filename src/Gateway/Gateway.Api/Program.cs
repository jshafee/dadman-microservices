using BuildingBlocks.Security;
using BuildingBlocks.ServiceDefaults;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServiceDefaults("gateway-api");
builder.Services.AddJwtSecurity(builder.Configuration);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("fixed", context =>
    {
        var key = GetRateLimitPartitionKey(context);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"fixed:{key}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 20,
                AutoReplenishment = true
            });
    });

    options.AddPolicy("write", context =>
    {
        var key = GetRateLimitPartitionKey(context);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"write:{key}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5,
                AutoReplenishment = true
            });
    });
});

builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("catalog-get", policy => policy
        .Expire(TimeSpan.FromSeconds(15))
        .SetVaryByQuery("api-version")
        .With(context => HttpMethods.IsGet(context.HttpContext.Request.Method)
            && context.HttpContext.Request.Path.StartsWithSegments("/catalog")));
});

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();
app.UseServiceDefaults();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseOutputCache();
app.MapReverseProxy().RequireAuthorization();
app.Run();

static string GetRateLimitPartitionKey(HttpContext context)
{
    var subject = context.User.FindFirst("sub")?.Value;
    if (!string.IsNullOrWhiteSpace(subject))
    {
        return $"user:{subject}";
    }

    var ipAddress = context.Connection.RemoteIpAddress?.ToString();
    return string.IsNullOrWhiteSpace(ipAddress) ? "ip:unknown" : $"ip:{ipAddress}";
}
