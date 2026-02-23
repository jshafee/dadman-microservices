using BuildingBlocks.Scoping;
using BuildingBlocks.Security;
using BuildingBlocks.ServiceDefaults;
using Notification.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);
builder.Services.AddServiceDefaults("notification-api");
builder.Services.AddJwtSecurity(builder.Configuration);
builder.Services.AddPlatformScoping();
builder.Services.AddNotificationInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseServiceDefaults();
app.UseAuthentication();
app.UsePlatformScopeContext();
app.UseAuthorization();

app.MapGet("/api/notify/v1/_ping", (IScopeContext scope) => Results.Ok(new { scope.TenantId, scope.ApplicationId, scope.PrincipalId, scope.CorrelationId, scope.CausationId })).RequireAuthorization();

app.Run();

public partial class Program { }
