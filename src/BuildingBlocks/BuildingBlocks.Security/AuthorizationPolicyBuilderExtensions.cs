using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BuildingBlocks.Security;

public static class AuthorizationPolicyBuilderExtensions
{
    public static AuthorizationPolicyBuilder RequireScope(this AuthorizationPolicyBuilder builder, string scope)
    {
        return builder.RequireAssertion(context => HasScope(context.User, scope));
    }

    private static bool HasScope(ClaimsPrincipal user, string scope)
    {
        foreach (var claim in user.FindAll("scope"))
        {
            var scopes = claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (scopes.Contains(scope, StringComparer.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
