using Microsoft.AspNetCore.Builder;

namespace BuildingBlocks.Security;

public static class MinimalApiAuthorizationExtensions
{
    public static TBuilder RequireCatalogReadScope<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequireAuthorization(SecurityPolicies.CatalogRead);

    public static TBuilder RequireCatalogWriteScope<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequireAuthorization(SecurityPolicies.CatalogWrite);

    public static TBuilder RequireOrderingReadScope<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequireAuthorization(SecurityPolicies.OrderingRead);

    public static TBuilder RequireOrderingWriteScope<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
        => builder.RequireAuthorization(SecurityPolicies.OrderingWrite);
}
