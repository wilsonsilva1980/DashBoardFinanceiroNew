namespace Financeiro.Domain.Entities;

public sealed class PlanningEntry : ScopedEntity
{
    public Guid TitleId { get; set; }
    public Guid PersonId { get; set; }
    public decimal Amount { get; set; }
}
