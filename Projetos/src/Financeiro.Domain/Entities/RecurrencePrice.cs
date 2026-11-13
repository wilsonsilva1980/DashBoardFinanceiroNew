namespace Financeiro.Domain.Entities;

public sealed class RecurrencePrice : ScopedEntity
{
    public Guid RecurrenceId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal CycleAmount { get; set; }
    public string Currency { get; set; } = "BRL";
    public int IntervalMonths { get; set; } = 1;
}
