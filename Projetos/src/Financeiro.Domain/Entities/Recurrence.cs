namespace Financeiro.Domain.Entities;

public sealed class Recurrence : ScopedEntity
{
    public Guid ObligationId { get; set; }
    public DateOnly? AnchorDate { get; set; }
    public int NominalDay { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool Active { get; set; } = true;
}
