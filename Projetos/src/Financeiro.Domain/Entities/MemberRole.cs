namespace Financeiro.Domain.Entities;

public sealed class MemberRole : ScopedEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}
