using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Scoping;

public static class ScopingServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformScoping(this IServiceCollection services)
    {
        services.AddScoped<ScopeContext>();
        services.AddScoped<IScopeContext>(sp => sp.GetRequiredService<ScopeContext>());
        return services;
    }
}
