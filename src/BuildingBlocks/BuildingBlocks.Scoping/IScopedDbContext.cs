namespace BuildingBlocks.Scoping;

public interface IScopedDbContext
{
    Guid? TenantId { get; }
    Guid? ApplicationId { get; }
}
