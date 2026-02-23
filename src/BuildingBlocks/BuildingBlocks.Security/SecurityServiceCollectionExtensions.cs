using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace BuildingBlocks.Security;

public static class SecurityServiceCollectionExtensions
{
    public static IServiceCollection AddJwtSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>()
            ?? throw new InvalidOperationException($"Missing '{AuthOptions.SectionName}' configuration section.");

        EnsureRequiredValue(authOptions.Issuer, nameof(AuthOptions.Issuer));
        EnsureRequiredValue(authOptions.Audience, nameof(AuthOptions.Audience));
        EnsureRequiredValue(authOptions.SigningKey, nameof(AuthOptions.SigningKey));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.SigningKey));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = authOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(SecurityPolicies.CatalogRead, policy => policy.RequireAuthenticatedUser().RequireScope(SecurityPolicies.CatalogRead));
            options.AddPolicy(SecurityPolicies.CatalogWrite, policy => policy.RequireAuthenticatedUser().RequireScope(SecurityPolicies.CatalogWrite));
            options.AddPolicy(SecurityPolicies.OrderingRead, policy => policy.RequireAuthenticatedUser().RequireScope(SecurityPolicies.OrderingRead));
            options.AddPolicy(SecurityPolicies.OrderingWrite, policy => policy.RequireAuthenticatedUser().RequireScope(SecurityPolicies.OrderingWrite));
            options.AddPolicy(SecurityPolicies.Authenticated, policy => policy.RequireAuthenticatedUser());
        });

        return services;
    }

    private static void EnsureRequiredValue(string value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing or empty '{AuthOptions.SectionName}:{key}' configuration value.");
        }
    }
}
