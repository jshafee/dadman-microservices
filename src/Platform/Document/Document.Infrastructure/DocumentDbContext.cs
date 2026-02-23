using BuildingBlocks.Scoping;
using Microsoft.EntityFrameworkCore;

namespace Document.Infrastructure;

public sealed class DocumentDbContext(DbContextOptions<DocumentDbContext> options, IScopeContext scopeContext) : DbContext(options)
{
    public DbSet<DocumentEntity> Entities => Set<DocumentEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200);
        });
        modelBuilder.Entity<DocumentEntity>().HasQueryFilter(x => x.TenantId == scopeContext.TenantId && x.ApplicationId == scopeContext.ApplicationId);
    }
}
