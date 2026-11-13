namespace Financeiro.Domain.Entities;

public sealed class Institution : ScopedEntity, INamedCatalog
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public bool Active { get; set; } = true;
}
