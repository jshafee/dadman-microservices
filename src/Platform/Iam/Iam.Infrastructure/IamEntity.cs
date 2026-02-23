using BuildingBlocks.Scoping;

namespace Iam.Infrastructure;

public sealed class IamUser : ITenantScoped, IApplicationScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ApplicationId { get; set; }
    public string ExternalSubject { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class IamGroup : ITenantScoped, IApplicationScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ApplicationId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class IamRole : ITenantScoped, IApplicationScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ApplicationId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class IamPermission : ITenantScoped, IApplicationScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ApplicationId { get; set; }
    public string Code { get; set; } = string.Empty;
}

public sealed class IamAssignment : ITenantScoped, IApplicationScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid PrincipalId { get; set; }
    public string PrincipalType { get; set; } = string.Empty;
    public Guid PermissionId { get; set; }
}
