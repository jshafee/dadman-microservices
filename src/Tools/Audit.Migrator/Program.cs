using BuildingBlocks.Scoping;
using Microsoft.EntityFrameworkCore;
using Audit.Infrastructure;

const string ConnectionStringVariableName = "ConnectionStrings__AuditDb";
const int MaxAttempts = 20;
var delay = TimeSpan.FromSeconds(3);

var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariableName);
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine($"Missing required environment variable '{ConnectionStringVariableName}'.");
    return 1;
}

var optionsBuilder = new DbContextOptionsBuilder<AuditDbContext>();
optionsBuilder.UseSqlServer(connectionString);

for (var attempt = 1; attempt <= MaxAttempts; attempt++)
{
    try
    {
        await using var dbContext = new AuditDbContext(optionsBuilder.Options, new ScopeContext());
        Console.WriteLine($"Applying AuditDb migrations (attempt {attempt}/{MaxAttempts})...");
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("AuditDb migrations applied successfully.");
        return 0;
    }
    catch (Exception ex) when (attempt < MaxAttempts)
    {
        Console.Error.WriteLine($"AuditDb migration attempt {attempt} failed: {ex.Message}");
        await Task.Delay(delay);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"AuditDb migration failed after {MaxAttempts} attempts: {ex}");
        return 1;
    }
}

return 1;
