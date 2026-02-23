namespace BuildingBlocks.Scoping;

public interface IScopeContext
{
    Guid? TenantId { get; }
    Guid? ApplicationId { get; }
    Guid? PrincipalId { get; }
    string CorrelationId { get; }
    string? CausationId { get; }
}
