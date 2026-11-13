namespace Financeiro.Domain.Entities;

public sealed class TravelItem : ScopedEntity
{
    public Guid TravelId { get; set; }
    public Guid? ObligationId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ResponsibleId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string State { get; set; } = "Budget";
    public string? Currency { get; set; }
    public decimal? BaseAmount { get; set; }
    public decimal? Fees { get; set; }
}
