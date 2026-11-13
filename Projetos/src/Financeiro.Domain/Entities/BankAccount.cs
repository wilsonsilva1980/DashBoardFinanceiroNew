namespace Financeiro.Domain.Entities;

public sealed class BankAccount : ScopedEntity
{
    public Guid InstitutionId { get; set; }
    public Guid? HolderId { get; set; }
    public string? Type { get; set; }
    public string? Agency { get; set; }
    public string? Number { get; set; }
    public string? CheckDigit { get; set; }
    public string Currency { get; set; } = "BRL";
    public decimal OverdraftLimit { get; set; }
    public bool Active { get; set; } = true;
}
