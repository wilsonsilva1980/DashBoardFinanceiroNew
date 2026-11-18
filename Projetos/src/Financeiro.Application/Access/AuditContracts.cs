namespace Financeiro.Application.Access;

public interface IAuditWriter
{
    void Record(string action, string entity, Guid entityId, object? changes = null,
        Guid? tenantId = null, Guid? spaceId = null, Guid? userId = null);
}

public static class AuditEventCatalog
{
    private static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["onboarding.confirmed"] = "Onboarding confirmado",
        ["space.member.deactivated"] = "Membro do espaço desativado",
        ["tenant.member.deactivated"] = "Membro do tenant desativado",
        ["pagamento.confirmado"] = "Pagamento confirmado",
        ["pagamento.revertido"] = "Pagamento revertido",
        ["liquidacao-historica.registrada"] = "Liquidação histórica registrada",
        ["liquidacao-historica.revogada"] = "Liquidação histórica revogada",
        ["acordo.confirmado"] = "Acordo confirmado",
        ["titulo.cancelado"] = "Título cancelado",
        ["titulo.excluido"] = "Título excluído",
        ["obrigacao.excluida"] = "Obrigação excluída",
        ["importacao.analisada"] = "Importação analisada",
        ["importacao.decisao"] = "Decisão de importação registrada",
        ["importacao.confirmada"] = "Importação confirmada",
        ["importacao.cancelada"] = "Importação cancelada"
    };

    public static IReadOnlyDictionary<string, string> All => Labels;

    public static bool IsKnown(string action) => Labels.ContainsKey(action);

    public static string Label(string action) => Labels.TryGetValue(action, out var label) ? label : action;
}

public sealed class AuditValidationException(string message) : ArgumentException(message);
