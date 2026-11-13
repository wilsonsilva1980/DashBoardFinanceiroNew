using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Financeiro.Application.Access;
using Financeiro.Domain.Entities;
using Financeiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Infrastructure.Access;

public sealed class OnboardingService(FinanceiroDbContext db, IRequestActor actor, IAuditWriter? auditWriter = null) : IOnboardingService
{
    public async Task<OnboardingResult> OnboardAsync(OnboardingRequest request, CancellationToken cancellationToken = default)
    {
        var userId = actor.UserId ?? throw new AccessDeniedException("Authentication required.");
        if (string.IsNullOrWhiteSpace(request.TenantName) || request.TenantName.Trim().Length > 120 ||
            string.IsNullOrWhiteSpace(request.SpaceName) || request.SpaceName.Trim().Length > 120 ||
            string.IsNullOrWhiteSpace(request.RequestKey) || request.RequestKey.Length > 100 ||
            !new[] { "BRL", "USD", "EUR", "CLP", "ARS", "GBP", "JPY", "CAD", "AUD", "CHF", "CNY", "UYU", "PYG", "BOB", "PEN", "COP", "MXN" }.Contains(request.Currency))
            throw new ArgumentException("Invalid onboarding data.");
        var canonical = JsonSerializer.Serialize(new { Tenant = request.TenantName.Trim(), Space = request.SpaceName.Trim(), request.Currency });
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // Serialize onboarding for this authenticated user, including concurrent retries.
        var user = db.Database.IsNpgsql()
            ? await db.Users.FromSqlInterpolated($"SELECT * FROM \"AspNetUsers\" WHERE \"id\" = {userId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
            : await db.Users.FromSqlInterpolated($"SELECT * FROM [AspNetUsers] WITH (UPDLOCK,HOLDLOCK) WHERE [id] = {userId}").SingleOrDefaultAsync(cancellationToken);
        if (user is null || !user.Active) throw new AccessDeniedException("Active user required.");
        using var provisioning = db.BeginProvisioning();
        var existing = await db.OnboardingRecords.SingleOrDefaultAsync(x => x.UserId == userId && x.RequestKey == request.RequestKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.PayloadHash != hash) throw new RequestConflictException("Idempotency key was already used with different data.");
            await transaction.CommitAsync(cancellationToken);
            return new OnboardingResult(existing.TenantId, existing.SpaceId);
        }
        var now = DateTimeOffset.UtcNow;
        foreach (var key in PermissionCatalog.All.Order(StringComparer.Ordinal))
        {
            var id = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes("financeiro.permission." + key)).AsSpan(0, 16));
            var module = key.Split('.')[0];
            if (db.Database.IsNpgsql())
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO permissoes (id, version, created_at, updated_at, key, description, module) VALUES ({id}, 1, {now}, {now}, {key}, {key}, {module}) ON CONFLICT (key) DO NOTHING", cancellationToken);
            else
                await db.Database.ExecuteSqlInterpolatedAsync($"IF NOT EXISTS (SELECT 1 FROM permissoes WITH (UPDLOCK,HOLDLOCK) WHERE [key] = {key}) INSERT INTO permissoes (id, version, created_at, updated_at, [key], description, module) VALUES ({id}, 1, {now}, {now}, {key}, {key}, {module})", cancellationToken);
        }
        var permissions = await db.Permissions.AsNoTracking().ToDictionaryAsync(x => x.Key, x => x.Id, cancellationToken);
        var tenant = new Tenant { Name = request.TenantName.Trim(), PublicIdentifier = Guid.NewGuid().ToString("N"), CreatedBy = userId };
        var space = new FinancialSpace { TenantId = tenant.Id, Name = request.SpaceName.Trim(), NormalizedName = request.SpaceName.Trim().ToUpperInvariant(), Currency = request.Currency, CreatedBy = userId };
        db.AddRange(tenant, space,
            new TenantMember { TenantId = tenant.Id, UserId = userId, Administrator = true, CreatedBy = userId },
            new SpaceMember { TenantId = tenant.Id, SpaceId = space.Id, UserId = userId, CreatedBy = userId },
            new SpacePreferences { TenantId = tenant.Id, SpaceId = space.Id, CreatedBy = userId });
        foreach (var (name, keys) in PermissionCatalog.BaselineRoles)
        {
            var role = new SpaceRole { TenantId = tenant.Id, SpaceId = space.Id, Name = name, NormalizedName = name.ToUpperInvariant(), Protected = name == "Administrador", CreatedBy = userId };
            db.Add(role);
            foreach (var key in keys)
                db.Add(new RolePermission { TenantId = tenant.Id, SpaceId = space.Id, RoleId = role.Id, PermissionId = permissions[key], CreatedBy = userId });
            if (name == "Administrador") db.Add(new MemberRole { TenantId = tenant.Id, SpaceId = space.Id, RoleId = role.Id, UserId = userId, CreatedBy = userId });
        }
        db.Add(new OnboardingRecord { UserId = userId, TenantId = tenant.Id, SpaceId = space.Id, RequestKey = request.RequestKey, PayloadHash = hash, CreatedBy = userId });
        Writer.Record("onboarding.confirmed", "Tenant", tenant.Id, tenantId: tenant.Id, spaceId: space.Id, userId: userId);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new OnboardingResult(tenant.Id, space.Id);
    }

    private IAuditWriter Writer => auditWriter ?? new AuditWriter(db, new FinancialScope(), actor);
}
