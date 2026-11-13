namespace Financeiro.Domain.Entities;

public sealed class AccountPosition : ScopedEntity
{
    public Guid AccountId { get; set; }
    public DateOnly PositionDate { get; set; }
    public decimal? Balance { get; set; }
    public decimal? SavingsBalance { get; set; }
    public decimal? OverdraftUsed { get; set; }
}
