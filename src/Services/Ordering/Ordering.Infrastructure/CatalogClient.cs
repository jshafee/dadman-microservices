using System.Net;

namespace Ordering.Infrastructure;

public interface ICatalogClient
{
    Task<bool> CatalogItemExistsAsync(Guid catalogItemId, CancellationToken cancellationToken);
}

public sealed class CatalogClient(HttpClient httpClient) : ICatalogClient
{
    public async Task<bool> CatalogItemExistsAsync(Guid catalogItemId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"/catalog/items/{catalogItemId}?api-version=1.0", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }
}
