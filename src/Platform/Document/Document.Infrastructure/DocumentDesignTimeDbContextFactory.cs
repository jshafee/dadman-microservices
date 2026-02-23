using BuildingBlocks.Scoping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Document.Infrastructure;

public sealed class DocumentDesignTimeDbContextFactory : IDesignTimeDbContextFactory<DocumentDbContext>
{
    public DocumentDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DocumentDb") ?? "Server=localhost,1433;Database=DocumentDb;User Id=sa;Password=__SET_VIA_ENV__;TrustServerCertificate=true";
        var optionsBuilder = new DbContextOptionsBuilder<DocumentDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new DocumentDbContext(optionsBuilder.Options, new ScopeContext());
    }
}
