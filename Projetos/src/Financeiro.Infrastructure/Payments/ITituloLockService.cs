namespace Financeiro.Infrastructure.Payments;

/// <summary>
/// Locks payable titles in deterministic identifier order so competing confirmations
/// serialize instead of silently double-settling. Provider-specific SQL lives behind this port.
/// </summary>
public interface ITituloLockService
{
    Task LockAsync(Guid tenantId, Guid spaceId, IReadOnlyList<Guid> titleIds, CancellationToken cancellationToken = default);
}
