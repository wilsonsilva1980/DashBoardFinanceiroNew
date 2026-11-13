namespace Financeiro.Application.Access;

public interface IRequestActor
{
    Guid? UserId { get; }
    string CorrelationId { get; }
}

public sealed record OnboardingRequest(string TenantName, string SpaceName, string Currency, string RequestKey);
public sealed record OnboardingResult(Guid TenantId, Guid SpaceId);

public interface IOnboardingService
{
    Task<OnboardingResult> OnboardAsync(OnboardingRequest request, CancellationToken cancellationToken = default);
}

public sealed class RequestConflictException(string message) : Exception(message);
public sealed class AccessDeniedException(string message) : Exception(message);

public static class PermissionCatalog
{
    public static readonly string[] All = [
        "dashboard.visualizar", "cadastros.visualizar", "cadastros.editar",
        "obrigacoes.visualizar", "obrigacoes.editar", "pagamentos.registrar",
        "pagamentos.estornar", "quitacoes.confirmar", "relatorios.exportar",
        "usuarios.gerenciar", "perfis.gerenciar", "auditoria.visualizar",
        "historico.registrar", "historico.revogar", "viagens.editar", "cotacoes.editar", "faturas.editar"
    ];

    public static IReadOnlyDictionary<string, string[]> BaselineRoles { get; } = new Dictionary<string, string[]>
    {
        ["Administrador"] = All,
        ["Gestor financeiro"] = All.Where(p => p is not ("usuarios.gerenciar" or "perfis.gerenciar" or "auditoria.visualizar")).ToArray(),
        ["Operador"] = ["dashboard.visualizar", "cadastros.visualizar", "cadastros.editar", "obrigacoes.visualizar", "obrigacoes.editar", "pagamentos.registrar", "viagens.editar", "cotacoes.editar", "faturas.editar"],
        ["Leitor"] = ["dashboard.visualizar", "cadastros.visualizar", "obrigacoes.visualizar"]
    };
}
