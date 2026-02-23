using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Scoping;

public sealed class ScopeContextMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext httpContext, ScopeContext scopeContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            scopeContext.TenantId = ParseGuid(httpContext.User, "tenant_id");
            scopeContext.ApplicationId = ParseGuid(httpContext.User, "application_id");
            scopeContext.PrincipalId = ParseGuid(httpContext.User, "principal_id");
        }

        var correlationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
        httpContext.Request.Headers["X-Correlation-ID"] = correlationId;
        httpContext.Response.Headers["X-Correlation-ID"] = correlationId;
        httpContext.Items["CorrelationId"] = correlationId;

        scopeContext.CorrelationId = correlationId;
        scopeContext.CausationId = httpContext.Request.Headers["X-Causation-ID"].FirstOrDefault();

        await next(httpContext);
    }

    private static Guid? ParseGuid(ClaimsPrincipal principal, string claimType)
    {
        var value = principal.FindFirstValue(claimType);
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }
}

public static class ScopeContextApplicationBuilderExtensions
{
    public static IApplicationBuilder UsePlatformScopeContext(this IApplicationBuilder app)
        => app.UseMiddleware<ScopeContextMiddleware>();
}
