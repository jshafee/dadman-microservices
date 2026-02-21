namespace Ordering.Application;

public record CreateOrderCommand(Guid CatalogItemId, int Quantity);
