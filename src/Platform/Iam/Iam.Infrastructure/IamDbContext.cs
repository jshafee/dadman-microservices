using BuildingBlocks.Scoping;
using Microsoft.EntityFrameworkCore;

namespace Iam.Infrastructure;

public sealed class IamDbContext : ScopedDbContext
{
    public IamDbContext(DbContextOptions<IamDbContext> options, IScopeContext scopeContext)
        : base(options, scopeContext)
    {
    }

    public DbSet<IamUser> Users => Set<IamUser>();
    public DbSet<IamGroup> Groups => Set<IamGroup>();
    public DbSet<IamRole> Roles => Set<IamRole>();
    public DbSet<IamPermission> Permissions => Set<IamPermission>();
    public DbSet<IamAssignment> Assignments => Set<IamAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IamUser>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExternalSubject).HasMaxLength(256).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.ApplicationId, x.ExternalSubject }).IsUnique();
        });

        modelBuilder.Entity<IamGroup>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
        });

        modelBuilder.Entity<IamRole>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
        });

        modelBuilder.Entity<IamPermission>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.ApplicationId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<IamAssignment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PrincipalType).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.ApplicationId, x.PrincipalId, x.PermissionId }).IsUnique();
        });

        modelBuilder.ApplyScopeQueryFilters(this);
    }
}
