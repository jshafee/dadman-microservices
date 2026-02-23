using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Registry.Infrastructure;

public sealed class RegistryDesignTimeDbContextFactory : IDesignTimeDbContextFactory<RegistryDbContext>
{
    public RegistryDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__RegistryDb") ?? "Server=localhost,1433;Database=RegistryDb;User Id=sa;Password=__SET_VIA_ENV__;TrustServerCertificate=true";
        var optionsBuilder = new DbContextOptionsBuilder<RegistryDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new RegistryDbContext(optionsBuilder.Options);
    }
}
