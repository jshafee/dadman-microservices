namespace Ordering.Infrastructure;

public sealed class CatalogServiceOptions
{
    public const string SectionName = "Services:Catalog";

    public string BaseUrl { get; init; } = "http://localhost:5101";
}
