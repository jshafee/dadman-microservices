namespace Ordering.Infrastructure;

public sealed class CatalogServiceOptions
{
    public const string SectionName = "Services:Catalog";

    public string BaseUrl { get; set; } = "http://localhost:5101";
    public string? ServiceToken { get; init; }
}
