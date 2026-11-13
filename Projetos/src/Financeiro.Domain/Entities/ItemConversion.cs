namespace Financeiro.Domain.Entities;

public sealed class ItemConversion : ScopedEntity
{
    public Guid ItemId { get; set; }
    public Guid? RateId { get; set; }
    public string DestinationCurrency { get; set; } = string.Empty;
    public string Mode { get; set; } = "Calculated";
    public decimal? ReportedAmount { get; set; }
    public string? DateContext { get; set; }
}
