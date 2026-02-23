using BuildingBlocks.Scoping;
using Iam.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

const int MaxAttempts = 20;
var delay = TimeSpan.FromSeconds(3);

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .AddKeyPerFile("/run/secrets", optional: true)
    .Build();

var connectionString = configuration.GetConnectionString("IamDb") ?? configuration["ConnectionStrings:IamDb"];
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Missing connection string for IamDb.");
    return 1;
}

var optionsBuilder = new DbContextOptionsBuilder<IamDbContext>();
optionsBuilder.UseSqlServer(connectionString);

for (var attempt = 1; attempt <= MaxAttempts; attempt++)
{
    try
    {
        await using var dbContext = new IamDbContext(optionsBuilder.Options, new ScopeContext());
        Console.WriteLine($"Applying IamDb migrations (attempt {attempt}/{MaxAttempts})...");
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("IamDb migrations applied successfully.");
        return 0;
    }
    catch (Exception ex) when (attempt < MaxAttempts)
    {
        Console.Error.WriteLine($"IamDb migration attempt {attempt} failed: {ex.Message}");
        await Task.Delay(delay);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"IamDb migration failed after {MaxAttempts} attempts: {ex}");
        return 1;
    }
}

return 1;
