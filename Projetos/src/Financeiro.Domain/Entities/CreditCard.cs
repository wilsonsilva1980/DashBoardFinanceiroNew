namespace Financeiro.Domain.Entities;

public sealed class CreditCard : ScopedEntity
{
    public Guid InstitutionId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? HolderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? DueDay { get; set; }
    public int? ClosingDay { get; set; }
    public decimal? CreditLimit { get; set; }
    public decimal? CashLimit { get; set; }
    public decimal? ForeignCashLimit { get; set; }
    public decimal? PixLimit { get; set; }
    public decimal? BillLimit { get; set; }
    public bool Active { get; set; } = true;
}
