using BuildingBlocks.Scoping;
using Microsoft.EntityFrameworkCore;

namespace Org.Infrastructure;

public sealed class OrgDbContext(DbContextOptions<OrgDbContext> options, IScopeContext scopeContext) : DbContext(options)
{
    public DbSet<OrgEntity> Entities => Set<OrgEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrgEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200);
        });
        modelBuilder.Entity<OrgEntity>().HasQueryFilter(x => x.TenantId == scopeContext.TenantId && x.ApplicationId == scopeContext.ApplicationId);
    }
}
