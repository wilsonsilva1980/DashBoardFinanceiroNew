namespace Financeiro.Domain.Entities;

public sealed class Obligation : ScopedEntity
{
    public Guid? CategoryId { get; set; }
    public Guid? ResponsibleId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Creditor { get; set; }
    public string Kind { get; set; } = "Single";
    public DateOnly? PurchaseDate { get; set; }
    public string? PurchaseDateContext { get; set; }
    public int? DueDay { get; set; }
    public int? InstallmentCount { get; set; }
    public string Currency { get; set; } = "BRL";
    public bool Active { get; set; } = true;
}
