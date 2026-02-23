using BuildingBlocks.Scoping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Org.Infrastructure;

public sealed class OrgDesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrgDbContext>
{
    public OrgDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__OrgDb") ?? "Server=localhost,1433;Database=OrgDb;User Id=sa;Password=__SET_VIA_ENV__;TrustServerCertificate=true";
        var optionsBuilder = new DbContextOptionsBuilder<OrgDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new OrgDbContext(optionsBuilder.Options, new ScopeContext());
    }
}
