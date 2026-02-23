using BuildingBlocks.Scoping;
using BuildingBlocks.Security;
using BuildingBlocks.ServiceDefaults;
using Iam.Infrastructure;

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

app.MapGet("/api/iam/v1/_ping", (IScopeContext scope) => Results.Ok(new { scope.TenantId, scope.ApplicationId, scope.PrincipalId, scope.CorrelationId, scope.CausationId })).RequireAuthorization();

app.MapPost("/api/iam/v1/tokens:exchange", (TokenExchangeRequest request, IConfiguration configuration) =>
{
    var signingKey = configuration["Auth:SigningKey"] ?? throw new InvalidOperationException("Missing Auth:SigningKey");
    var issuer = configuration["Auth:Issuer"] ?? throw new InvalidOperationException("Missing Auth:Issuer");
    var audience = configuration["Auth:Audience"] ?? throw new InvalidOperationException("Missing Auth:Audience");
    var now = DateTime.UtcNow;
    var expires = now.AddMinutes(15);
    var principalId = Guid.NewGuid();
    var claims = new[]
    {
        new System.Security.Claims.Claim("sub", request.ExternalSubject),
        new System.Security.Claims.Claim("principal_id", principalId.ToString()),
        new System.Security.Claims.Claim("tenant_id", request.TenantId.ToString()),
        new System.Security.Claims.Claim("application_id", request.ApplicationId.ToString()),
        new System.Security.Claims.Claim("permission_snapshot_version", "0")
    };
    var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(signingKey)), Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(issuer: issuer, audience: audience, claims: claims, notBefore: now, expires: expires, signingCredentials: credentials);
    var accessToken = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new TokenExchangeResponse(accessToken, (int)TimeSpan.FromMinutes(15).TotalSeconds));
}).RequireAuthorization();

app.Run();

public partial class Program { }
