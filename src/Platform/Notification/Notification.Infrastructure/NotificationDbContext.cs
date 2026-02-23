using BuildingBlocks.Scoping;
using Microsoft.EntityFrameworkCore;

namespace Notification.Infrastructure;

public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options, IScopeContext scopeContext) : DbContext(options)
{
    public DbSet<NotificationEntity> Entities => Set<NotificationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200);
        });
        modelBuilder.Entity<NotificationEntity>().HasQueryFilter(x => x.TenantId == scopeContext.TenantId && x.ApplicationId == scopeContext.ApplicationId);
    }
}
