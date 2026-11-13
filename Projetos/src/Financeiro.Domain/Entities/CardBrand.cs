namespace Financeiro.Domain.Entities;

public sealed class CardBrand : ScopedEntity, INamedCatalog
{
    public bool Active { get; set; } = true;
    public string Name { get; set; } = string.Empty;
}
