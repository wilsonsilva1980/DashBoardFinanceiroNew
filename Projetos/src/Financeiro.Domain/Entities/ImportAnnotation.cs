namespace Financeiro.Domain.Entities;

public sealed class ImportAnnotation : ScopedEntity
{
    public Guid ImportBatchId { get; set; }
    public string SheetName { get; set; } = string.Empty;
    public string CellReference { get; set; } = string.Empty;
    public string? SourceAuthor { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset? SourceDate { get; set; }
}
