namespace Financeiro.Domain.Entities;

public sealed class ScenarioTitle : ScopedEntity
{
    public Guid ScenarioId { get; set; }
    public Guid TitleId { get; set; }
    public decimal NominalAmount { get; set; }
    public decimal Discount { get; set; }
}
