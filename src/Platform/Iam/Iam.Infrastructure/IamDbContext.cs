using BuildingBlocks.Scoping;
using Microsoft.EntityFrameworkCore;

namespace Iam.Infrastructure;

public sealed class IamDbContext(DbContextOptions<IamDbContext> options, IScopeContext scopeContext) : DbContext(options)
{
    public DbSet<IamEntity> Entities => Set<IamEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IamEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200);
        });
        modelBuilder.Entity<IamEntity>().HasQueryFilter(x => x.TenantId == scopeContext.TenantId && x.ApplicationId == scopeContext.ApplicationId);
    }
}
