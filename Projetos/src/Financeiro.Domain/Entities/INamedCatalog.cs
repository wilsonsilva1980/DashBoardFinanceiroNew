namespace Financeiro.Domain.Entities;

public interface INamedCatalog
{
    string Name { get; set; }
    bool Active { get; set; }
}
