namespace Financeiro.Domain.Entities;

public sealed class Annotation : ScopedEntity
{
    public Guid AuthorId { get; set; }
    public string Text { get; set; } = string.Empty;
    public Guid? TitleId { get; set; }
    public Guid? ObligationId { get; set; }
    public Guid? TravelId { get; set; }
}
