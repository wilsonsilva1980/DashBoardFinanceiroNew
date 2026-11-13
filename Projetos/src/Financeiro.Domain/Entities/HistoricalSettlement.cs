namespace Financeiro.Domain.Entities;

public sealed class HistoricalSettlement : ScopedEntity
{
    public Guid TitleId { get; set; }
    public Guid? PayerId { get; set; }
    public Guid ApprovedBy { get; set; }
    public string Justification { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly? EffectiveDate { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevocationReason { get; set; }
    public string? PreviousReference { get; set; }
    public string? SourceStatus { get; set; }
}
