namespace BuildingBlocks.Scoping;

public sealed class ScopeContext : IScopeContext
{
    public Guid? TenantId { get; set; }
    public Guid? ApplicationId { get; set; }
    public Guid? PrincipalId { get; set; }
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
    public string? CausationId { get; set; }
}
