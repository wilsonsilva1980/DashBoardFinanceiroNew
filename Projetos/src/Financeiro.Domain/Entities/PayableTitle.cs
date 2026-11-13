namespace Financeiro.Domain.Entities;

public sealed class PayableTitle : ScopedEntity
{
    public Guid ObligationId { get; set; }
    public Guid? ResponsibleId { get; set; }
    public Guid? CardId { get; set; }
    public DateOnly Period { get; set; }
    public DateOnly? DueDate { get; set; }
    public int? InstallmentNumber { get; set; }
    public string OccurrenceKey { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public TitleState State { get; set; } = TitleState.Draft;
    public DateTimeOffset? CancelledAt { get; set; }

    public bool CanTransitionTo(TitleState next) => State != TitleState.Cancelled && (int)next >= (int)State;
}
