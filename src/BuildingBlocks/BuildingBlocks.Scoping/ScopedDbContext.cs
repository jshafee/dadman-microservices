using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Scoping;

public abstract class ScopedDbContext : DbContext, IScopedDbContext
{
    protected ScopedDbContext(DbContextOptions options, IScopeContext scopeContext)
        : base(options)
    {
        TenantId = scopeContext.TenantId;
        ApplicationId = scopeContext.ApplicationId;
    }

    public Guid? TenantId { get; protected set; }
    public Guid? ApplicationId { get; protected set; }
}
