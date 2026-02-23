namespace Iam.Infrastructure;

public sealed class IamEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "placeholder";
    public Guid TenantId { get; set; }
    public Guid ApplicationId { get; set; }
}
