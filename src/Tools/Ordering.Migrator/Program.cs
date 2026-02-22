using Microsoft.EntityFrameworkCore;
using Ordering.Infrastructure;

const string ConnectionStringVariableName = "ConnectionStrings__OrderingDb";
const int MaxAttempts = 20;
var delay = TimeSpan.FromSeconds(3);

var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariableName);
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine($"Missing required environment variable '{ConnectionStringVariableName}'.");
    return 1;
}

var optionsBuilder = new DbContextOptionsBuilder<OrderingDbContext>();
optionsBuilder.UseSqlServer(connectionString);

for (var attempt = 1; attempt <= MaxAttempts; attempt++)
{
    try
    {
        await using var dbContext = new OrderingDbContext(optionsBuilder.Options);
        Console.WriteLine($"Applying OrderingDb migrations (attempt {attempt}/{MaxAttempts})...");
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("OrderingDb migrations applied successfully.");
        return 0;
    }
    catch (Exception ex) when (attempt < MaxAttempts)
    {
        Console.Error.WriteLine($"OrderingDb migration attempt {attempt} failed: {ex.Message}");
        await Task.Delay(delay);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"OrderingDb migration failed after {MaxAttempts} attempts: {ex}");
        return 1;
    }
}

return 1;
