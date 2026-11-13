namespace Financeiro.Domain.Entities;

public sealed class SettlementScenario : ScopedEntity
{
    public Guid ObligationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly SimulationDate { get; set; }
    public decimal ProposedAmount { get; set; }
    public string? Notes { get; set; }
}
