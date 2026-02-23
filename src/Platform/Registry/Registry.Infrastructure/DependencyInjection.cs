using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Registry.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRegistryInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("RegistryDb") ?? "Server=localhost,1433;Database=RegistryDb;User Id=sa;Password=__SET_VIA_ENV__;TrustServerCertificate=true";

        services.AddDbContext<RegistryDbContext>(options => options.UseSqlServer(connectionString, sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()));
        services.AddHealthChecks().AddSqlServer(connectionString, name: "sqlserver", tags: ["ready"]);
        return services;
    }
}
