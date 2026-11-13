using System.Text.Json;
using Financeiro.Application.Catalogs;

namespace Financeiro.Application.Access;

public sealed record AuditQuery(
    DateOnly? From = null,
    DateOnly? Until = null,
    string? Action = null,
    string? Entity = null,
    Guid? EntityId = null,
    Guid? UserId = null,
    string? CorrelationId = null,
    int Page = 1,
    int PageSize = 25);

public sealed record AuditEntry(
    Guid Id,
    DateTimeOffset Instant,
    string Action,
    string ActionLabel,
    string Entity,
    Guid EntityId,
    Guid? UserId,
    string? UserDisplayName,
    string CorrelationId,
    JsonElement Changes);

public interface IAuditQueryService
{
    Task<PageResult<AuditEntry>> ListAsync(AuditQuery query, CancellationToken cancellationToken = default);
    IReadOnlyDictionary<string, string> Events();
}
