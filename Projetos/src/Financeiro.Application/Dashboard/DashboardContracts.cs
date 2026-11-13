using Financeiro.Application.Travel;

namespace Financeiro.Application.Dashboard;

public sealed record DashboardAmountState(string Effective, string Forecast, string Open, string Partial, string Paid, string Overdue);
public sealed record DashboardCategorySummary(Guid? CategoryId, string CategoryName, DashboardAmountState Amounts);
public sealed record DashboardResponsibleSummary(Guid? ResponsibleId, string ResponsibleName, DashboardAmountState Amounts);
public sealed record DashboardDueDateSummary(DateOnly? DueDate, string Label, DashboardAmountState Amounts);
public sealed record DashboardMonthlyEvolution(DateOnly Month, string Effective, string Forecast, string Open, string Paid, string Overdue);
public sealed record DashboardAccountPositionSummary(Guid AccountId, string AccountLabel, DateOnly? PositionDate, string? Balance, string? SavingsBalance, string? OverdraftUsed, string ConfiguredOverdraftLimit, string? AvailableOverdraft);
public sealed record DashboardReconciliationIndicator(string Kind, string Scope, string Message, string? Amount = null, Guid? TitleId = null, Guid? TravelBudgetId = null);
public sealed record DashboardSummary(
    DateOnly From,
    DateOnly Until,
    string Currency,
    string? Income,
    DashboardAmountState Totals,
    IReadOnlyList<DashboardMonthlyEvolution> MonthlyEvolution,
    IReadOnlyList<DashboardCategorySummary> ByCategory,
    IReadOnlyList<DashboardResponsibleSummary> ByResponsible,
    IReadOnlyList<DashboardDueDateSummary> ByDueDate,
    IReadOnlyList<DashboardAccountPositionSummary> Positions,
    IReadOnlyList<DashboardReconciliationIndicator> ReconciliationIndicators);

public sealed record CashFlowSummary(DateOnly From, DateOnly Until, string Currency, string CashOutflow, string? Income);

public interface IDashboardQueries
{
    Task<CashFlowSummary> CashFlowAsync(DateOnly from, DateOnly until, string currency, CancellationToken cancellationToken = default);
    Task<DashboardSummary> SummaryAsync(DateOnly from, DateOnly until, string currency, CancellationToken cancellationToken = default);
}
