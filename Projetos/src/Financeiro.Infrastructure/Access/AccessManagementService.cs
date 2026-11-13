using Financeiro.Application.Access;
using Financeiro.Domain.Entities;
using Financeiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Infrastructure.Access;

public sealed class AccessManagementService(FinanceiroDbContext db, FinancialScope scope, IRequestActor actor, PermissionEvaluator permissions, IAuditWriter? auditWriter = null)
{
    public async Task DeactivateSpaceMemberAsync(Guid userId, long expectedVersion, CancellationToken cancellationToken = default)
    {
        if (!scope.IsAuthorized) throw new AccessDeniedException("Active financial context required.");
        var spaceId = scope.SpaceId!.Value;
        var tenantId = scope.TenantId!.Value;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        _ = db.Database.IsNpgsql()
            ? await db.FinancialSpaces.FromSqlInterpolated($"SELECT * FROM espacos WHERE id={spaceId} AND tenant_id={tenantId} FOR UPDATE").SingleAsync(cancellationToken)
            : await db.FinancialSpaces.FromSqlInterpolated($"SELECT * FROM espacos WITH (UPDLOCK,HOLDLOCK) WHERE id={spaceId} AND tenant_id={tenantId}").SingleAsync(cancellationToken);
        if (!await permissions.HasAsync("usuarios.gerenciar", cancellationToken)) throw new AccessDeniedException("Membership management permission required.");
        var member = await db.SpaceMembers.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken) ?? throw new AccessDeniedException("Member unavailable.");
        if (member.Version != expectedVersion) throw new RequestConflictException("Membership changed.");
        var administratorIds = from binding in db.MemberRoles
                               join role in db.SpaceRoles on binding.RoleId equals role.Id
                               where role.Active && role.Protected && role.NormalizedName == "ADMINISTRADOR"
                               select binding.UserId;
        if (member.Active && await administratorIds.AnyAsync(x => x == userId, cancellationToken) &&
            await db.SpaceMembers.CountAsync(x => x.Active && administratorIds.Contains(x.UserId), cancellationToken) <= 1)
            throw new RequestConflictException("Cannot deactivate the final space administrator.");
        member.Active = false;
        member.AccessVersion++;
        Writer.Record("space.member.deactivated", "Membership", member.Id);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeactivateTenantMemberAsync(Guid userId, long expectedVersion, CancellationToken cancellationToken = default)
    {
        if (!scope.IsAuthorized || actor.UserId is not Guid actorId) throw new AccessDeniedException("Active context required.");
        var tenantId = scope.TenantId!.Value;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        _ = db.Database.IsNpgsql()
            ? await db.Tenants.FromSqlInterpolated($"SELECT * FROM tenants WHERE id={tenantId} FOR UPDATE").SingleAsync(cancellationToken)
            : await db.Tenants.FromSqlInterpolated($"SELECT * FROM tenants WITH (UPDLOCK,HOLDLOCK) WHERE id={tenantId}").SingleAsync(cancellationToken);
        if (!await db.TenantMembers.AnyAsync(x => x.TenantId == tenantId && x.UserId == actorId && x.Active && x.Administrator, cancellationToken))
            throw new AccessDeniedException("Tenant administration permission required.");
        var member = await db.TenantMembers.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == userId, cancellationToken) ?? throw new AccessDeniedException("Member unavailable.");
        if (member.Version != expectedVersion) throw new RequestConflictException("Membership changed.");
        if (member.Active && member.Administrator && await db.TenantMembers.CountAsync(x => x.TenantId == tenantId && x.Active && x.Administrator, cancellationToken) <= 1)
            throw new RequestConflictException("Cannot deactivate the final tenant administrator.");
        member.Active = false;
        Writer.Record("tenant.member.deactivated", "Membership", member.Id);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private IAuditWriter Writer => auditWriter ?? new AuditWriter(db, scope, actor);
}
