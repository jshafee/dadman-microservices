using Microsoft.AspNetCore.OutputCaching;
using System.Security.Claims;

namespace Gateway.Api;

public sealed class CatalogOutputCachePolicy : IOutputCachePolicy
{
    public static readonly CatalogOutputCachePolicy Instance = new();

    private CatalogOutputCachePolicy()
    {
    }

    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        var request = context.HttpContext.Request;
        if (!IsCacheableRequest(request))
        {
            return ValueTask.CompletedTask;
        }

        context.EnableOutputCaching = true;
        context.AllowCacheLookup = true;
        context.AllowCacheStorage = true;
        context.AllowLocking = true;

        context.CacheVaryByRules.QueryKeys = new[] { "api-version" };

        var subject = context.HttpContext.User.FindFirst("sub")?.Value
            ?? context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? "anonymous";
        context.CacheVaryByRules.VaryByValues["subject"] = subject;

        context.ResponseExpirationTimeSpan = TimeSpan.FromSeconds(15);
        return ValueTask.CompletedTask;
    }

    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        var response = context.HttpContext.Response;

        if (response.StatusCode != StatusCodes.Status200OK)
        {
            context.AllowCacheStorage = false;
            return ValueTask.CompletedTask;
        }

        if (response.Headers.SetCookie.Count > 0)
        {
            context.AllowCacheStorage = false;
        }

        return ValueTask.CompletedTask;
    }

    private static bool IsCacheableRequest(HttpRequest request)
    {
        if (!request.Path.StartsWithSegments("/catalog", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method);
    }
}
