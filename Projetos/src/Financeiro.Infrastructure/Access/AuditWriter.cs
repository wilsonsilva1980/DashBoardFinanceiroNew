using System.Text.Json;
using System.Text.Json.Nodes;
using Financeiro.Application.Access;
using Financeiro.Domain.Entities;
using Financeiro.Infrastructure.Persistence;

namespace Financeiro.Infrastructure.Access;

public sealed class AuditWriter(FinanceiroDbContext db, FinancialScope scope, IRequestActor actor) : IAuditWriter
{
    private const int MaxChangesLength = 8000;
    private static readonly string[] SensitiveNames = ["password", "passwd", "token", "cookie", "secret", "credential", "accountnumber", "bankaccountnumber", "agency", "checkdigit"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Record(string action, string entity, Guid entityId, object? changes = null,
        Guid? tenantId = null, Guid? spaceId = null, Guid? userId = null)
    {
        if (string.IsNullOrWhiteSpace(action) || action.Length > 200 || !AuditEventCatalog.IsKnown(action))
            throw new AuditValidationException("Ação de auditoria inválida.");
        if (string.IsNullOrWhiteSpace(entity) || entity.Length > 200)
            throw new AuditValidationException("Entidade de auditoria inválida.");
        if (entityId == Guid.Empty)
            throw new AuditValidationException("Identificador de entidade inválido.");

        var resolvedTenant = tenantId ?? scope.TenantId;
        var resolvedSpace = spaceId ?? scope.SpaceId;
        if (resolvedTenant is not Guid tenant || resolvedSpace is not Guid space)
            throw new AccessDeniedException("Financial scope is required for audit.");

        var json = SafeJson(changes);
        db.AuditRecords.Add(new AuditRecord
        {
            TenantId = tenant,
            SpaceId = space,
            UserId = userId ?? actor.UserId,
            CreatedBy = userId ?? actor.UserId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Instant = DateTimeOffset.UtcNow,
            CorrelationId = actor.CorrelationId,
            ChangesJson = json
        });
    }

    private static string SafeJson(object? changes)
    {
        if (changes is null) return "{}";
        JsonNode? node;
        try
        {
            var raw = changes is string text ? text : JsonSerializer.Serialize(changes, JsonOptions);
            node = JsonNode.Parse(raw);
        }
        catch (JsonException)
        {
            throw new AuditValidationException("Detalhes de auditoria inválidos.");
        }

        Redact(node);
        var result = node?.ToJsonString(JsonOptions) ?? "{}";
        if (result.Length > MaxChangesLength)
            throw new AuditValidationException("Detalhes de auditoria excederam o tamanho permitido.");
        return result;
    }

    private static void Redact(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj.ToArray())
            {
                var normalizedName = Normalize(property.Key);
                if (SensitiveNames.Any(name => normalizedName.Contains(name, StringComparison.OrdinalIgnoreCase)))
                    obj[property.Key] = "[REDACTED]";
                else
                    Redact(property.Value);
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array) Redact(item);
        }
    }

    private static string Normalize(string value) => new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
}
