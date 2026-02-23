namespace BuildingBlocks.Scoping;

public interface ITenantScoped
{
    Guid TenantId { get; }
}

public interface IApplicationScoped
{
    Guid ApplicationId { get; }
}
