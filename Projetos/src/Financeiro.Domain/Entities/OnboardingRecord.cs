namespace Financeiro.Domain.Entities;

public sealed class OnboardingRecord : Entity
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public Guid SpaceId { get; set; }
    public string RequestKey { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
}
