namespace Financeiro.Infrastructure.Identity;

public sealed class UserSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastAccessAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset AbsoluteExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string SecurityVersion { get; set; } = string.Empty;
    public Guid? ActiveTenantId { get; set; }
    public Guid? ActiveSpaceId { get; set; }
}
