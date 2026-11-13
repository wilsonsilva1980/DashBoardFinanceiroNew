namespace Financeiro.Domain.Entities;

public sealed class TravelBudget : ScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}
