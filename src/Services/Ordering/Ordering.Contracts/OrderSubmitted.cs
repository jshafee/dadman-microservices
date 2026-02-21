namespace Ordering.Contracts;

public record OrderSubmitted(Guid OrderId, Guid CatalogItemId, int Quantity);
