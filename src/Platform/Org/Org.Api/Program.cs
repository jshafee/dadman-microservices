using BuildingBlocks.Scoping;
using BuildingBlocks.Security;
using BuildingBlocks.ServiceDefaults;
using Org.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);
builder.Services.AddServiceDefaults("org-api");
builder.Services.AddJwtSecurity(builder.Configuration);
builder.Services.AddPlatformScoping();
builder.Services.AddOrgInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseServiceDefaults();
app.UseAuthentication();
app.UsePlatformScopeContext();
app.UseAuthorization();

app.MapGet("/api/org/v1/_ping", (IScopeContext scope) => Results.Ok(new { scope.TenantId, scope.ApplicationId, scope.PrincipalId, scope.CorrelationId, scope.CausationId })).RequireAuthorization();

app.Run();

public partial class Program { }
