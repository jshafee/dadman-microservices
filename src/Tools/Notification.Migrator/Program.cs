using BuildingBlocks.Scoping;
using Microsoft.EntityFrameworkCore;
using Notification.Infrastructure;

const string ConnectionStringVariableName = "ConnectionStrings__NotificationDb";
const int MaxAttempts = 20;
var delay = TimeSpan.FromSeconds(3);

var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariableName);
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine($"Missing required environment variable '{ConnectionStringVariableName}'.");
    return 1;
}

var optionsBuilder = new DbContextOptionsBuilder<NotificationDbContext>();
optionsBuilder.UseSqlServer(connectionString);

for (var attempt = 1; attempt <= MaxAttempts; attempt++)
{
    try
    {
        await using var dbContext = new NotificationDbContext(optionsBuilder.Options, new ScopeContext());
        Console.WriteLine($"Applying NotificationDb migrations (attempt {attempt}/{MaxAttempts})...");
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("NotificationDb migrations applied successfully.");
        return 0;
    }
    catch (Exception ex) when (attempt < MaxAttempts)
    {
        Console.Error.WriteLine($"NotificationDb migration attempt {attempt} failed: {ex.Message}");
        await Task.Delay(delay);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"NotificationDb migration failed after {MaxAttempts} attempts: {ex}");
        return 1;
    }
}

return 1;
