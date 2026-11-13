namespace Financeiro.Domain.Entities;

public sealed class ExchangeRate : ScopedEntity
{
    public string SourceCurrency { get; set; } = string.Empty;
    public string DestinationCurrency { get; set; } = string.Empty;
    public DateOnly? RateDate { get; set; }
    public string? DateContext { get; set; }
    public decimal Rate { get; set; }
    public string Source { get; set; } = string.Empty;
}
