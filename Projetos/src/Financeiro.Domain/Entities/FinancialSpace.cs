namespace Financeiro.Domain.Entities;

public sealed class FinancialSpace : Entity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string TimeZone { get; set; } = "America/Sao_Paulo";
    public string Currency { get; set; } = "BRL";
    public bool Active { get; set; } = true;
}
