namespace Ordering.Domain;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CatalogItemId { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
