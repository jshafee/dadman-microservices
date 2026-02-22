using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ordering.Infrastructure;

public sealed class OrderingDesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrderingDbContext>
{
    public OrderingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__OrderingDb")
            ?? "Server=localhost,1433;Database=OrderingDb;User Id=sa;Password=__SET_VIA_ENV__;TrustServerCertificate=true";

        var optionsBuilder = new DbContextOptionsBuilder<OrderingDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new OrderingDbContext(optionsBuilder.Options);
    }
}
