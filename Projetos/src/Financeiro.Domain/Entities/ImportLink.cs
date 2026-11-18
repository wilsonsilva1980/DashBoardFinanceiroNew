namespace Financeiro.Domain.Entities;

public sealed class ImportLink : ScopedEntity
{
    public Guid ImportBatchId { get; set; }
    public Guid SourceRecordId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string OccurrenceKey { get; set; } = string.Empty;
}
