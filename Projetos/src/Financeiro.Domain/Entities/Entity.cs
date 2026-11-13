namespace Financeiro.Domain.Entities;

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long Version { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
}

public abstract class ScopedEntity : Entity
{
    public Guid TenantId { get; set; }
    public Guid SpaceId { get; set; }
}

public enum TitleState { Draft, Forecast, Effective, Cancelled }
public enum PaymentState { Draft, Confirmed, Reversed }
