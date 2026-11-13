namespace Financeiro.Domain.Entities;

public sealed class RolePermission : ScopedEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}
