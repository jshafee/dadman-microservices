using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Headers;

namespace Ordering.Infrastructure;

public sealed class ServiceTokenHandler(
    IHttpContextAccessor httpContextAccessor,
    IOptions<CatalogServiceOptions> catalogOptions,
    IWebHostEnvironment environment) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var serviceToken = catalogOptions.Value.ServiceToken;
        if (!string.IsNullOrWhiteSpace(serviceToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceToken);
            return base.SendAsync(request, cancellationToken);
        }

        if (environment.IsDevelopment())
        {
            var authHeader = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(authHeader)
                && AuthenticationHeaderValue.TryParse(authHeader, out var parsedHeader))
            {
                request.Headers.Authorization = parsedHeader;
            }

            return base.SendAsync(request, cancellationToken);
        }

        throw new InvalidOperationException(
            $"Missing required configuration value '{CatalogServiceOptions.SectionName}:ServiceToken' for outbound Catalog service authentication.");
    }
}
