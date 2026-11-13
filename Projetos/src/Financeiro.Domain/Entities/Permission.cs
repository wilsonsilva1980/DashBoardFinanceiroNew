namespace Financeiro.Domain.Entities;

public sealed class Permission : Entity
{
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
}
