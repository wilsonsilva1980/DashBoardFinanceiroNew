using System.Text.Json;
using Financeiro.Application.Access;
using Financeiro.Application.Catalogs;
using Financeiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Infrastructure.Access;

public sealed class AuditQueryService(FinanceiroDbContext db, FinancialScope scope, PermissionEvaluator permissions) : IAuditQueryService
{
    public async Task<PageResult<AuditEntry>> ListAsync(AuditQuery input, CancellationToken cancellationToken = default)
    {
        if (!scope.IsAuthorized || !await permissions.HasAsync("auditoria.visualizar", cancellationToken))
            throw new AccessDeniedException("Permissão de auditoria necessária no espaço ativo.");
        var page = new PageRequest(input.Page, input.PageSize);
        var offset = page.Offset();
        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = input.From ?? now.AddDays(-30);
        var until = input.Until ?? now;
        if (until < from || until.DayNumber - from.DayNumber > 366)
            throw new CatalogValidationException("Informe um intervalo de auditoria válido de até 367 dias.");
        if (input.Action is not null && !AuditEventCatalog.IsKnown(input.Action))
            throw new CatalogValidationException("Ação de auditoria não suportada.");
        if (input.Entity?.Length > 200 || input.CorrelationId?.Length > 200)
            throw new CatalogValidationException("Filtro de auditoria excedeu o tamanho permitido.");

        var fromInstant = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var untilExclusive = new DateTimeOffset(until.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var query = from audit in db.AuditRecords.AsNoTracking()
                    join user in db.Users.AsNoTracking() on audit.UserId equals user.Id into users
                    from user in users.DefaultIfEmpty()
                    where audit.Instant >= fromInstant && audit.Instant < untilExclusive
                    select new { audit, UserDisplayName = user == null ? null : user.DisplayName };
        if (!string.IsNullOrWhiteSpace(input.Action)) query = query.Where(x => x.audit.Action == input.Action);
        if (!string.IsNullOrWhiteSpace(input.Entity)) query = query.Where(x => x.audit.Entity == input.Entity);
        if (input.EntityId is Guid entityId) query = query.Where(x => x.audit.EntityId == entityId);
        if (input.UserId is Guid userId) query = query.Where(x => x.audit.UserId == userId);
        if (!string.IsNullOrWhiteSpace(input.CorrelationId)) query = query.Where(x => x.audit.CorrelationId == input.CorrelationId);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.audit.Instant).ThenByDescending(x => x.audit.Id)
            .Skip(offset).Take(page.PageSize).ToArrayAsync(cancellationToken);
        var entries = rows.Select(x => new AuditEntry(
            x.audit.Id,
            x.audit.Instant,
            x.audit.Action,
            AuditEventCatalog.Label(x.audit.Action),
            x.audit.Entity,
            x.audit.EntityId,
            x.audit.UserId,
            x.UserDisplayName,
            x.audit.CorrelationId,
            ParseChanges(x.audit.ChangesJson))).ToArray();
        return new(entries, total, page.Page, page.PageSize);
    }

    public IReadOnlyDictionary<string, string> Events() => AuditEventCatalog.All;

    private static JsonElement ParseChanges(string json)
    {
        try { return JsonSerializer.Deserialize<JsonElement>(json); }
        catch (JsonException) { return JsonSerializer.Deserialize<JsonElement>("{}"); }
    }
}
