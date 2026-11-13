namespace Financeiro.Domain.Entities;

public sealed class CardInvoice : ScopedEntity
{
    public Guid CardId { get; set; }
    public Guid TitleId { get; set; }
    public DateOnly Period { get; set; }
    public string Mode { get; set; } = "Consolidated";
    public decimal? ReportedAmount { get; set; }
    public string? Notes { get; set; }
}
