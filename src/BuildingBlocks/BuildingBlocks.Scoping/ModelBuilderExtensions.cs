using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Scoping;

public static class ModelBuilderExtensions
{
    public static void ApplyScopeQueryFilters(this ModelBuilder modelBuilder, ScopedDbContext dbContext)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (!typeof(ITenantScoped).IsAssignableFrom(clrType))
            {
                continue;
            }

            var entityParameter = Expression.Parameter(clrType, "e");
            var dbContextExpression = Expression.Constant(dbContext);

            var tenantProperty = Expression.Property(entityParameter, nameof(ITenantScoped.TenantId));
            var tenantScope = Expression.Property(dbContextExpression, nameof(ScopedDbContext.TenantId));
            var tenantFilter = Expression.Equal(tenantProperty, Expression.Property(tenantScope, nameof(Nullable<Guid>.Value)));
            var tenantHasValue = Expression.Property(tenantScope, nameof(Nullable<Guid>.HasValue));
            Expression body = Expression.AndAlso(tenantHasValue, tenantFilter);

            if (typeof(IApplicationScoped).IsAssignableFrom(clrType))
            {
                var appProperty = Expression.Property(entityParameter, nameof(IApplicationScoped.ApplicationId));
                var appScope = Expression.Property(dbContextExpression, nameof(ScopedDbContext.ApplicationId));
                var appFilter = Expression.Equal(appProperty, Expression.Property(appScope, nameof(Nullable<Guid>.Value)));
                var appHasValue = Expression.Property(appScope, nameof(Nullable<Guid>.HasValue));
                body = Expression.AndAlso(body, Expression.AndAlso(appHasValue, appFilter));
            }

            var lambda = Expression.Lambda(body, entityParameter);
            modelBuilder.Entity(clrType).HasQueryFilter(lambda);
        }
    }
}
