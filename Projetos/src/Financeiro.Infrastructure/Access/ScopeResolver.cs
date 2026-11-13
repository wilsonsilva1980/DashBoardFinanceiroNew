using Financeiro.Application.Access;
using Financeiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Infrastructure.Access;

public sealed class ScopeResolver(FinanceiroDbContext db, FinancialScope scope)
{
    public async Task<bool> ResolveAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken = default)
    {
        scope.Clear();
        var now = DateTimeOffset.UtcNow;
        var session = await db.UserSessions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId && x.RevokedAt == null && x.ExpiresAt > now && x.AbsoluteExpiresAt > now, cancellationToken);
        if (session?.ActiveTenantId is not Guid tenant || session.ActiveSpaceId is not Guid space) return false;
        var stamp = await db.Users.Where(x => x.Id == userId).Select(x => x.SecurityStamp).SingleOrDefaultAsync(cancellationToken);
        if (!string.Equals(stamp, session.SecurityVersion, StringComparison.Ordinal)) return false;
        if (!await IsMemberAsync(userId, tenant, space, cancellationToken)) return false;
        scope.Authorize(tenant, space);
        return true;
    }

    public async Task SelectAsync(Guid sessionId, Guid userId, Guid tenantId, Guid spaceId, CancellationToken cancellationToken = default)
    {
        if (!await IsMemberAsync(userId, tenantId, spaceId, cancellationToken)) throw new AccessDeniedException("Financial space unavailable.");
        var now = DateTimeOffset.UtcNow;
        var session = await db.UserSessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId && x.RevokedAt == null && x.ExpiresAt > now && x.AbsoluteExpiresAt > now, cancellationToken)
            ?? throw new AccessDeniedException("Active session required.");
        var stamp = await db.Users.Where(x => x.Id == userId).Select(x => x.SecurityStamp).SingleOrDefaultAsync(cancellationToken);
        if (!string.Equals(stamp, session.SecurityVersion, StringComparison.Ordinal))
            throw new AccessDeniedException("Session security version changed.");
        session.ActiveTenantId = tenantId;
        session.ActiveSpaceId = spaceId;
        await db.SaveChangesAsync(cancellationToken);
        scope.Authorize(tenantId, spaceId);
    }

    private async Task<bool> IsMemberAsync(Guid userId, Guid tenantId, Guid spaceId, CancellationToken cancellationToken)
    {
        return await db.Users.AsNoTracking().AnyAsync(x => x.Id == userId && x.Active, cancellationToken)
            && await db.Tenants.AsNoTracking().AnyAsync(x => x.Id == tenantId && x.Status == "Active", cancellationToken)
            && await db.FinancialSpaces.AsNoTracking().AnyAsync(x => x.Id == spaceId && x.TenantId == tenantId && x.Active, cancellationToken)
            && await db.TenantMembers.AsNoTracking().AnyAsync(x => x.UserId == userId && x.TenantId == tenantId && x.Active, cancellationToken)
            && await db.SpaceMembers.IgnoreQueryFilters().AsNoTracking().AnyAsync(x => x.UserId == userId && x.TenantId == tenantId && x.SpaceId == spaceId && x.Active, cancellationToken);
    }
}
