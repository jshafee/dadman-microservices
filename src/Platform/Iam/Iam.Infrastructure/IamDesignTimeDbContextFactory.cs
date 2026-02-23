using BuildingBlocks.Scoping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Iam.Infrastructure;

public sealed class IamDesignTimeDbContextFactory : IDesignTimeDbContextFactory<IamDbContext>
{
    public IamDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__IamDb") ?? "Server=localhost,1433;Database=IamDb;User Id=sa;Password=__SET_VIA_ENV__;TrustServerCertificate=true";
        var optionsBuilder = new DbContextOptionsBuilder<IamDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new IamDbContext(optionsBuilder.Options, new ScopeContext());
    }
}
