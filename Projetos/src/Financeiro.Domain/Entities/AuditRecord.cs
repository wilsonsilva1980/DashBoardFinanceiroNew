namespace Financeiro.Domain.Entities;

public sealed class AuditRecord : ScopedEntity
{
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public DateTimeOffset Instant { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string ChangesJson { get; set; } = "{}";
}
