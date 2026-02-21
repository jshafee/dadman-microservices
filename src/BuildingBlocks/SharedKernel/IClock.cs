namespace SharedKernel;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
