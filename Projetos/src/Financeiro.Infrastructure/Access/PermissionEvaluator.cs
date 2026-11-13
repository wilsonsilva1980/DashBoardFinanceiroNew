using Financeiro.Application.Access;
using Financeiro.Domain.Entities;
using Financeiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Infrastructure.Access;

public sealed class PermissionEvaluator(FinanceiroDbContext db, FinancialScope scope, IRequestActor actor)
{
    private async Task<Guid?> ActiveUserAsync(CancellationToken cancellationToken)
    {
        if (!scope.IsAuthorized || actor.UserId is not Guid userId) return null;
        if (!await db.Users.AnyAsync(x => x.Id == userId && x.Active, cancellationToken)
            || !await db.Tenants.AnyAsync(x => x.Id == scope.TenantId && x.Status == "Active", cancellationToken)
            || !await db.FinancialSpaces.AnyAsync(x => x.Id == scope.SpaceId && x.TenantId == scope.TenantId && x.Active, cancellationToken)
            || !await db.TenantMembers.AnyAsync(x => x.UserId == userId && x.TenantId == scope.TenantId && x.Active, cancellationToken)
            || !await db.SpaceMembers.AnyAsync(x => x.UserId == userId && x.Active, cancellationToken)) return null;
        return userId;
    }
    private IQueryable<Permission> Grants(Guid userId) =>
        from memberRole in db.MemberRoles.AsNoTracking()
        join role in db.SpaceRoles.AsNoTracking() on memberRole.RoleId equals role.Id
        join rolePermission in db.RolePermissions.AsNoTracking() on role.Id equals rolePermission.RoleId
        join granted in db.Permissions.AsNoTracking() on rolePermission.PermissionId equals granted.Id
        where memberRole.UserId == userId && role.Active
        select granted;

    public async Task<bool> HasAsync(string permission, CancellationToken cancellationToken = default)
    {
        if (!PermissionCatalog.All.Contains(permission)) return false;
        return await ActiveUserAsync(cancellationToken) is Guid userId && await Grants(userId).AnyAsync(x => x.Key == permission, cancellationToken);
    }
    public async Task<IReadOnlyList<string>> EffectiveAsync(CancellationToken cancellationToken = default)
    {
        if (await ActiveUserAsync(cancellationToken) is not Guid userId) return [];
        return await Grants(userId).Where(x => PermissionCatalog.All.Contains(x.Key)).Select(x => x.Key).Distinct().OrderBy(x => x).ToArrayAsync(cancellationToken);
    }
}
