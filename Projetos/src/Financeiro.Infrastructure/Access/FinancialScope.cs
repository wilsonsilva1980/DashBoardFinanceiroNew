namespace Financeiro.Infrastructure.Access;

/// <summary>Request-scoped state populated only after server-side membership checks.</summary>
public sealed class FinancialScope
{
    public Guid? TenantId { get; private set; }
    public Guid? SpaceId { get; private set; }
    public bool IsAuthorized => TenantId.HasValue && SpaceId.HasValue;
    public bool IsMaintenance { get; private init; }

    public static FinancialScope ForMaintenance() => new() { IsMaintenance = true };

    internal void Authorize(Guid tenantId, Guid spaceId)
    {
        TenantId = tenantId;
        SpaceId = spaceId;
    }

    internal void Clear()
    {
        TenantId = null;
        SpaceId = null;
    }
}
