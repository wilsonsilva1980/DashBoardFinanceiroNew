namespace Financeiro.Domain.Entities;

public sealed class Payment : ScopedEntity
{
    public Guid AccountId { get; set; }
    public Guid PayerId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BRL";
    public string Method { get; set; } = string.Empty;
    public PaymentState State { get; set; } = PaymentState.Draft;
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset? ReversedAt { get; set; }
    public string? ReversalReason { get; set; }
}
