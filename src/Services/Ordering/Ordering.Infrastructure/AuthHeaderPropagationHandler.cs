using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace Ordering.Infrastructure;

public sealed class AuthHeaderPropagationHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authHeader)
            && AuthenticationHeaderValue.TryParse(authHeader, out var parsedHeader))
        {
            request.Headers.Authorization = parsedHeader;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
