namespace Financeiro.Domain.Entities;

public sealed class SpaceMember : ScopedEntity
{
    public Guid UserId { get; set; }
    public bool Active { get; set; } = true;
    public long AccessVersion { get; set; } = 1;
}
