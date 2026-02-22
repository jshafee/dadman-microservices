using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catalog.Infrastructure;

public sealed class CatalogDesignTimeDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__CatalogDb")
            ?? "Server=localhost,1433;Database=CatalogDb;User Id=sa;Password=__SET_VIA_ENV__;TrustServerCertificate=true";

        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new CatalogDbContext(optionsBuilder.Options);
    }
}
