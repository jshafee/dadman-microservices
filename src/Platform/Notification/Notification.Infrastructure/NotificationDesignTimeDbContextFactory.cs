using BuildingBlocks.Scoping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Notification.Infrastructure;

public sealed class NotificationDesignTimeDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__NotificationDb") ?? "Server=localhost,1433;Database=NotificationDb;User Id=sa;Password=__SET_VIA_ENV__;TrustServerCertificate=true";
        var optionsBuilder = new DbContextOptionsBuilder<NotificationDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new NotificationDbContext(optionsBuilder.Options, new ScopeContext());
    }
}
