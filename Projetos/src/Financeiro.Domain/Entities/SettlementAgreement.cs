namespace Financeiro.Domain.Entities;

public sealed class SettlementAgreement : ScopedEntity
{
    public Guid ObligationId { get; set; }
    public DateOnly? ProposalDate { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public decimal ProposedAmount { get; set; }
    public string State { get; set; } = "Draft";
    public Guid? PaymentId { get; set; }
}
