namespace Financeiro.Domain.Entities;

public sealed class Category : ScopedEntity, INamedCatalog
{
    public string Name { get; set; } = string.Empty;
    public string Nature { get; set; } = "Unclassified";
    public bool Active { get; set; } = true;
}
