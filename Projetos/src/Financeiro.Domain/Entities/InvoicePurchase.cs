namespace Financeiro.Domain.Entities;

public sealed class InvoicePurchase : ScopedEntity
{
    public Guid InvoiceId { get; set; }
    public Guid? PurchaseObligationId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int? InstallmentNumber { get; set; }
    public string OccurrenceKey { get; set; } = string.Empty;
}
