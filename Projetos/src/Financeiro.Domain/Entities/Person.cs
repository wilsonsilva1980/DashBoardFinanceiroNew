namespace Financeiro.Domain.Entities;

public sealed class Person : ScopedEntity, INamedCatalog
{
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
}
