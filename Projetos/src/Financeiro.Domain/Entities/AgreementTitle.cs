namespace Financeiro.Domain.Entities;

public sealed class AgreementTitle : ScopedEntity
{
    public Guid AgreementId { get; set; }
    public Guid TitleId { get; set; }
    public decimal NominalAmount { get; set; }
    public decimal Discount { get; set; }
}
