namespace Financeiro.Domain.Entities;

public sealed class TenantMember : Entity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public bool Active { get; set; } = true;
    public bool Administrator { get; set; }
}
