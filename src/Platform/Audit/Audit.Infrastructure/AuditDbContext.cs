using BuildingBlocks.Scoping;
using Microsoft.EntityFrameworkCore;

namespace Audit.Infrastructure;

public sealed class AuditDbContext(DbContextOptions<AuditDbContext> options, IScopeContext scopeContext) : DbContext(options)
{
    public DbSet<AuditEntity> Entities => Set<AuditEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200);
        });
        modelBuilder.Entity<AuditEntity>().HasQueryFilter(x => x.TenantId == scopeContext.TenantId && x.ApplicationId == scopeContext.ApplicationId);
    }
}
