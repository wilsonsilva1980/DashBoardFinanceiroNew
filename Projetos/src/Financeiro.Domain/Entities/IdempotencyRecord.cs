namespace Financeiro.Domain.Entities;

public sealed class IdempotencyRecord : ScopedEntity
{
    public Guid ActorId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string RequestKey { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public Guid ResultId { get; set; }
}
