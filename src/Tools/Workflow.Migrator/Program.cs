using BuildingBlocks.Scoping;
using Microsoft.EntityFrameworkCore;
using Workflow.Infrastructure;

const string ConnectionStringVariableName = "ConnectionStrings__WorkflowDb";
const int MaxAttempts = 20;
var delay = TimeSpan.FromSeconds(3);

var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariableName);
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine($"Missing required environment variable '{ConnectionStringVariableName}'.");
    return 1;
}

var optionsBuilder = new DbContextOptionsBuilder<WorkflowDbContext>();
optionsBuilder.UseSqlServer(connectionString);

for (var attempt = 1; attempt <= MaxAttempts; attempt++)
{
    try
    {
        await using var dbContext = new WorkflowDbContext(optionsBuilder.Options, new ScopeContext());
        Console.WriteLine($"Applying WorkflowDb migrations (attempt {attempt}/{MaxAttempts})...");
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("WorkflowDb migrations applied successfully.");
        return 0;
    }
    catch (Exception ex) when (attempt < MaxAttempts)
    {
        Console.Error.WriteLine($"WorkflowDb migration attempt {attempt} failed: {ex.Message}");
        await Task.Delay(delay);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"WorkflowDb migration failed after {MaxAttempts} attempts: {ex}");
        return 1;
    }
}

return 1;
