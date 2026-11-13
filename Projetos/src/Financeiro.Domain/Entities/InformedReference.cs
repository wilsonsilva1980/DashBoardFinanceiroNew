namespace Financeiro.Domain.Entities;

public sealed class InformedReference : ScopedEntity
{
    public string Label { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public Guid? TitleId { get; set; }
    public Guid? ObligationId { get; set; }
    public Guid? TravelId { get; set; }
}
