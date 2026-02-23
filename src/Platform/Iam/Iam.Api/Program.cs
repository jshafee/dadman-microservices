using BuildingBlocks.Scoping;
using BuildingBlocks.Security;
using BuildingBlocks.ServiceDefaults;
using Iam.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);
builder.Services.AddServiceDefaults("iam-api");
builder.Services.AddJwtSecurity(builder.Configuration);
builder.Services.AddPlatformScoping();
builder.Services.AddIamInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseServiceDefaults();
app.UseAuthentication();
app.UsePlatformScopeContext();
app.UseAuthorization();

app.MapGet("/api/iam/v1/_ping", (IScopeContext scope) => Results.Ok(new
{
    service = "iam-api",
    tenantId = scope.TenantId,
    applicationId = scope.ApplicationId,
    principalId = scope.PrincipalId,
    correlationId = scope.CorrelationId
})).RequireAuthorization();

app.MapPost("/api/iam/v1/tokens:exchange", (TokenExchangeRequest request, IConfiguration configuration, IWebHostEnvironment environment) =>
{
    if (!environment.IsDevelopment())
    {
        return Results.NotFound();
    }

    var signingKey = configuration["Auth:SigningKey"] ?? throw new InvalidOperationException("Missing Auth:SigningKey");
    var issuer = configuration["Auth:Issuer"] ?? throw new InvalidOperationException("Missing Auth:Issuer");
    var audience = configuration["Auth:Audience"] ?? throw new InvalidOperationException("Missing Auth:Audience");

    var now = DateTime.UtcNow;
    var expiresInSeconds = 900;
    var expires = now.AddSeconds(expiresInSeconds);
    var principalId = Guid.NewGuid();

    var claims = new[]
    {
        new Claim("sub", request.ExternalSubject),
        new Claim("principal_id", principalId.ToString()),
        new Claim("tenant_id", request.TenantId.ToString()),
        new Claim("application_id", request.ApplicationId.ToString()),
        new Claim("permission_snapshot_version", "0")
    };

    var credentials = new SigningCredentials(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
        SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: claims,
        notBefore: now,
        expires: expires,
        signingCredentials: credentials);

    var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new TokenExchangeResponse(accessToken, expiresInSeconds));
}).AllowAnonymous();

app.Run();

public partial class Program { }
