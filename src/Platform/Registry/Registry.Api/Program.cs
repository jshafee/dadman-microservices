using BuildingBlocks.Scoping;
using BuildingBlocks.Security;
using BuildingBlocks.ServiceDefaults;
using Registry.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);
builder.Services.AddServiceDefaults("registry-api");
builder.Services.AddJwtSecurity(builder.Configuration);
builder.Services.AddPlatformScoping();
builder.Services.AddRegistryInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseServiceDefaults();
app.UseAuthentication();
app.UsePlatformScopeContext();
app.UseAuthorization();

app.MapGet("/api/platform-registry/v1/_ping", (IScopeContext scope) => Results.Ok(new
{
    service = "registry-api",
    tenantId = scope.TenantId,
    applicationId = scope.ApplicationId,
    principalId = scope.PrincipalId,
    correlationId = scope.CorrelationId
})).RequireAuthorization();

app.Run();

public partial class Program { }
