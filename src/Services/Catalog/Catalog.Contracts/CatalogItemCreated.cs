namespace Catalog.Contracts;

public record CatalogItemCreated(Guid ItemId, string Name, decimal Price);
