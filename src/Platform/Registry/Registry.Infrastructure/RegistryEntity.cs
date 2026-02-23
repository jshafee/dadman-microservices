namespace Registry.Infrastructure;

public sealed class RegistryEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "placeholder";
}
