namespace Fip.SharedKernel;

public abstract class Entity
{
    public Guid Id { get; protected init; } = Guid.NewGuid();
}
