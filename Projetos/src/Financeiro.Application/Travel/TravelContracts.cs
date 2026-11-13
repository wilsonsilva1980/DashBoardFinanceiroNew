using Financeiro.Application.Catalogs;

namespace Financeiro.Application.Travel;

public sealed record TravelBudgetInput(string Name, DateOnly? StartDate, DateOnly? EndDate, long? Version = null);
public sealed record TravelBudgetEntry(Guid Id, string Name, DateOnly? StartDate, DateOnly? EndDate, long Version);

public sealed record TravelItemInput(Guid TravelId, Guid? ObligationId, Guid? CategoryId, Guid? ResponsibleId, string Description, string State, string? Currency, decimal? BaseAmount, decimal? Fees, long? Version = null);
public sealed record TravelItemEntry(Guid Id, Guid TravelId, Guid? ObligationId, Guid? CategoryId, Guid? ResponsibleId, string Description, string State, string? Currency, string? BaseAmount, string? Fees, long Version);

public sealed record ExchangeRateInput(string SourceCurrency, string DestinationCurrency, DateOnly? RateDate, string? DateContext, decimal Rate, string Source, long? Version = null);
public sealed record ExchangeRateEntry(Guid Id, string SourceCurrency, string DestinationCurrency, DateOnly? RateDate, string? DateContext, string Rate, string Source, long Version);

public sealed record ItemConversionInput(Guid ItemId, Guid? RateId, string DestinationCurrency, string Mode, decimal? ReportedAmount, string? DateContext, long? Version = null);
public sealed record ItemConversionEntry(Guid Id, Guid ItemId, Guid? RateId, string DestinationCurrency, string Mode, string? ReportedAmount, string? DateContext, string? AppliedRate, DateOnly? AppliedRateDate, string? AppliedRateSource, long Version);

public sealed record TravelDiscrepancy(string Kind, string Message, string? Amount = null, Guid? ItemId = null, Guid? ConversionId = null, Guid? ObligationId = null);
public sealed record TravelBudgetReconciliation(Guid BudgetId, string? Balance, IReadOnlyList<TravelDiscrepancy> Discrepancies);

public interface ITravelService
{
    Task<PageResult<TravelBudgetEntry>> ListBudgetsAsync(PageRequest page, CancellationToken cancellationToken = default);
    Task<TravelBudgetEntry> GetBudgetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TravelBudgetEntry> SaveBudgetAsync(Guid? id, TravelBudgetInput input, CancellationToken cancellationToken = default);

    Task<PageResult<TravelItemEntry>> ListItemsAsync(Guid travelId, PageRequest page, CancellationToken cancellationToken = default);
    Task<TravelItemEntry> SaveItemAsync(Guid? id, TravelItemInput input, CancellationToken cancellationToken = default);

    Task<PageResult<ExchangeRateEntry>> ListRatesAsync(PageRequest page, CancellationToken cancellationToken = default);
    Task<ExchangeRateEntry> SaveRateAsync(Guid? id, ExchangeRateInput input, CancellationToken cancellationToken = default);

    Task<PageResult<ItemConversionEntry>> ListConversionsAsync(Guid itemId, PageRequest page, CancellationToken cancellationToken = default);
    Task<ItemConversionEntry> SaveConversionAsync(Guid? id, ItemConversionInput input, CancellationToken cancellationToken = default);

    Task<TravelBudgetReconciliation> GetBudgetReconciliationAsync(Guid travelId, CancellationToken cancellationToken = default);
}
