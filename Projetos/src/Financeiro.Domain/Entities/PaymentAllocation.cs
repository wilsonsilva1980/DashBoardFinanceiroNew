namespace Financeiro.Domain.Entities;

public sealed class PaymentAllocation : ScopedEntity
{
    public Guid PaymentId { get; set; }
    public Guid TitleId { get; set; }
    public decimal Amount { get; set; }
}
