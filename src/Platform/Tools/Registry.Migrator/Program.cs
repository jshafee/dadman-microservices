using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Registry.Infrastructure;

const int MaxAttempts = 20;
var delay = TimeSpan.FromSeconds(3);

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .AddKeyPerFile("/run/secrets", optional: true)
    .Build();

var connectionString = configuration.GetConnectionString("RegistryDb") ?? configuration["ConnectionStrings:RegistryDb"];
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Missing connection string for RegistryDb.");
    return 1;
}

var optionsBuilder = new DbContextOptionsBuilder<RegistryDbContext>();
optionsBuilder.UseSqlServer(connectionString);

for (var attempt = 1; attempt <= MaxAttempts; attempt++)
{
    try
    {
        await using var dbContext = new RegistryDbContext(optionsBuilder.Options);
        Console.WriteLine($"Applying RegistryDb migrations (attempt {attempt}/{MaxAttempts})...");
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("RegistryDb migrations applied successfully.");
        return 0;
    }
    catch (Exception ex) when (attempt < MaxAttempts)
    {
        Console.Error.WriteLine($"RegistryDb migration attempt {attempt} failed: {ex.Message}");
        await Task.Delay(delay);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"RegistryDb migration failed after {MaxAttempts} attempts: {ex}");
        return 1;
    }
}

return 1;
