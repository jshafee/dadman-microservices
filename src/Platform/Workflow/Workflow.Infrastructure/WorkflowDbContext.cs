using BuildingBlocks.Scoping;
using Microsoft.EntityFrameworkCore;

namespace Workflow.Infrastructure;

public sealed class WorkflowDbContext(DbContextOptions<WorkflowDbContext> options, IScopeContext scopeContext) : DbContext(options)
{
    public DbSet<WorkflowEntity> Entities => Set<WorkflowEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200);
        });
        modelBuilder.Entity<WorkflowEntity>().HasQueryFilter(x => x.TenantId == scopeContext.TenantId && x.ApplicationId == scopeContext.ApplicationId);
    }
}
