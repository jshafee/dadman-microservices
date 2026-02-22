using Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

const string ConnectionStringVariableName = "ConnectionStrings__CatalogDb";
const int MaxAttempts = 20;
var delay = TimeSpan.FromSeconds(3);

var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariableName);
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine($"Missing required environment variable '{ConnectionStringVariableName}'.");
    return 1;
}

var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
optionsBuilder.UseSqlServer(connectionString);

for (var attempt = 1; attempt <= MaxAttempts; attempt++)
{
    try
    {
        await using var dbContext = new CatalogDbContext(optionsBuilder.Options);
        Console.WriteLine($"Applying CatalogDb migrations (attempt {attempt}/{MaxAttempts})...");
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("CatalogDb migrations applied successfully.");
        return 0;
    }
    catch (Exception ex) when (attempt < MaxAttempts)
    {
        Console.Error.WriteLine($"CatalogDb migration attempt {attempt} failed: {ex.Message}");
        await Task.Delay(delay);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"CatalogDb migration failed after {MaxAttempts} attempts: {ex}");
        return 1;
    }
}

return 1;
