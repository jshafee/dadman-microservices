using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Notification.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("NotificationDb") ?? "Server=localhost,1433;Database=NotificationDb;User Id=sa;Password=__SET_VIA_ENV__;TrustServerCertificate=true";

        services.AddDbContext<NotificationDbContext>(options => options.UseSqlServer(connectionString, sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()));
        services.AddHealthChecks().AddSqlServer(connectionString, name: "sqlserver", tags: ["ready"]);
        return services;
    }
}
