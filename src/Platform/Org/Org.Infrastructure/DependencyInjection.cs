using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Org.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrgInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OrgDb") ?? "Server=localhost,1433;Database=OrgDb;User Id=sa;Password=__SET_VIA_ENV__;TrustServerCertificate=true";

        services.AddDbContext<OrgDbContext>(options => options.UseSqlServer(connectionString, sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()));
        services.AddHealthChecks().AddSqlServer(connectionString, name: "sqlserver", tags: ["ready"]);
        return services;
    }
}
