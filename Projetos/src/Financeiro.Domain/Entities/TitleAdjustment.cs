namespace Financeiro.Domain.Entities;

public sealed class TitleAdjustment : ScopedEntity
{
    public Guid TitleId { get; set; }
    public Guid? AgreementId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
