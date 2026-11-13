using Financeiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Infrastructure.Payments;

public sealed class PostgreSqlTitleLockService(FinanceiroDbContext db) : ITituloLockService
{
    public async Task LockAsync(Guid tenantId, Guid spaceId, IReadOnlyList<Guid> titleIds, CancellationToken cancellationToken = default)
    {
        foreach (var id in titleIds.OrderBy(x => x))
            await db.Database.ExecuteSqlAsync($"SELECT id FROM titulos WHERE tenant_id = {tenantId} AND espaco_id = {spaceId} AND id = {id} FOR UPDATE", cancellationToken);
    }
}
