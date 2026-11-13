using System.Globalization;
using Financeiro.Application.Access;
using Financeiro.Application.Catalogs;
using Financeiro.Application.Travel;
using Financeiro.Domain;
using Financeiro.Domain.Entities;
using Financeiro.Infrastructure.Access;
using Financeiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Infrastructure.Travel;

public sealed class TravelService(FinanceiroDbContext db, FinancialScope scope, IRequestActor actor, PermissionEvaluator permissions) : ITravelService
{
    public async Task<PageResult<TravelBudgetEntry>> ListBudgetsAsync(PageRequest page, CancellationToken cancellationToken = default)
    {
        await DemandAsync(false, cancellationToken);
        var offset = page.Offset();
        var query = db.TravelBudgets.AsNoTracking().OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.Skip(offset).Take(page.PageSize).ToArrayAsync(cancellationToken);
        return new(rows.Select(BudgetEntry).ToArray(), total, page.Page, page.PageSize);
    }

    public async Task<TravelBudgetEntry> GetBudgetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await DemandAsync(false, cancellationToken);
        var entity = await db.TravelBudgets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new CatalogNotFoundException();
        return BudgetEntry(entity);
    }

    public async Task<TravelBudgetEntry> SaveBudgetAsync(Guid? id, TravelBudgetInput input, CancellationToken cancellationToken = default)
    {
        await DemandAsync(true, cancellationToken);
        var name = CatalogValidation.Name(input.Name, 150);
        if (input.StartDate is { } start && input.EndDate is { } end && end < start) throw new CatalogValidationException("A data final deve ser posterior ou igual à data inicial.");
        var entity = id.HasValue ? await db.TravelBudgets.SingleOrDefaultAsync(x => x.Id == id.Value, cancellationToken) : null;
        if (id.HasValue)
        {
            if (entity is null) throw new CatalogNotFoundException();
            CatalogValidation.Version(input.Version, entity.Version);
        }
        entity ??= new TravelBudget { TenantId = scope.TenantId!.Value, SpaceId = scope.SpaceId!.Value, CreatedBy = actor.UserId };
        entity.Name = name;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        if (!id.HasValue) db.TravelBudgets.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return BudgetEntry(entity);
    }

    public async Task<PageResult<TravelItemEntry>> ListItemsAsync(Guid travelId, PageRequest page, CancellationToken cancellationToken = default)
    {
        await DemandAsync(false, cancellationToken);
        _ = await db.TravelBudgets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == travelId, cancellationToken) ?? throw new CatalogNotFoundException();
        var offset = page.Offset();
        var query = db.TravelItems.AsNoTracking().Where(x => x.TravelId == travelId).OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.Skip(offset).Take(page.PageSize).ToArrayAsync(cancellationToken);
        return new(rows.Select(ItemEntry).ToArray(), total, page.Page, page.PageSize);
    }

    public async Task<TravelItemEntry> SaveItemAsync(Guid? id, TravelItemInput input, CancellationToken cancellationToken = default)
    {
        await DemandAsync(true, cancellationToken);
        _ = await db.TravelBudgets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == input.TravelId, cancellationToken) ?? throw new CatalogNotFoundException();
        var description = CatalogValidation.Name(input.Description, 300);
        CatalogValidation.Money(input.BaseAmount);
        CatalogValidation.Money(input.Fees);
        if (!string.IsNullOrWhiteSpace(input.Currency)) CatalogValidation.Currency(input.Currency!.Trim().ToUpperInvariant());
        var entity = id.HasValue ? await db.TravelItems.SingleOrDefaultAsync(x => x.Id == id.Value, cancellationToken) : null;
        if (id.HasValue)
        {
            if (entity is null) throw new CatalogNotFoundException();
            CatalogValidation.Version(input.Version, entity.Version);
        }
        entity ??= new TravelItem { TenantId = scope.TenantId!.Value, SpaceId = scope.SpaceId!.Value, CreatedBy = actor.UserId, TravelId = input.TravelId };
        entity.TravelId = input.TravelId;
        entity.ObligationId = input.ObligationId;
        entity.CategoryId = input.CategoryId;
        entity.ResponsibleId = input.ResponsibleId;
        entity.Description = description;
        entity.State = string.IsNullOrWhiteSpace(input.State) ? "Budget" : input.State.Trim();
        entity.Currency = string.IsNullOrWhiteSpace(input.Currency) ? null : input.Currency.Trim().ToUpperInvariant();
        entity.BaseAmount = input.BaseAmount;
        entity.Fees = input.Fees;
        if (!id.HasValue) db.TravelItems.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ItemEntry(entity);
    }

    public async Task<PageResult<ExchangeRateEntry>> ListRatesAsync(PageRequest page, CancellationToken cancellationToken = default)
    {
        await DemandAsync(false, cancellationToken);
        var offset = page.Offset();
        var query = db.ExchangeRates.AsNoTracking().OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.Skip(offset).Take(page.PageSize).ToArrayAsync(cancellationToken);
        return new(rows.Select(RateEntry).ToArray(), total, page.Page, page.PageSize);
    }

    public async Task<ExchangeRateEntry> SaveRateAsync(Guid? id, ExchangeRateInput input, CancellationToken cancellationToken = default)
    {
        await DemandAsync(true, cancellationToken);
        var source = input.SourceCurrency?.Trim().ToUpperInvariant();
        var destination = input.DestinationCurrency?.Trim().ToUpperInvariant();
        CatalogValidation.Currency(source);
        CatalogValidation.Currency(destination);
        if (input.Rate <= 0 || input.Rate > 99999999999999999.99m) throw new CatalogValidationException("Informe uma cotação positiva válida.");
        var entity = id.HasValue ? await db.ExchangeRates.SingleOrDefaultAsync(x => x.Id == id.Value, cancellationToken) : null;
        if (id.HasValue)
        {
            if (entity is null) throw new CatalogNotFoundException();
            CatalogValidation.Version(input.Version, entity.Version);
        }
        entity ??= new ExchangeRate { TenantId = scope.TenantId!.Value, SpaceId = scope.SpaceId!.Value, CreatedBy = actor.UserId };
        entity.SourceCurrency = source!;
        entity.DestinationCurrency = destination!;
        entity.RateDate = input.RateDate;
        entity.DateContext = string.IsNullOrWhiteSpace(input.DateContext) ? null : input.DateContext.Trim();
        entity.Rate = input.Rate;
        entity.Source = CatalogValidation.Name(input.Source, 200);
        if (!id.HasValue) db.ExchangeRates.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return RateEntry(entity);
    }

    public async Task<PageResult<ItemConversionEntry>> ListConversionsAsync(Guid itemId, PageRequest page, CancellationToken cancellationToken = default)
    {
        await DemandAsync(false, cancellationToken);
        _ = await db.TravelItems.AsNoTracking().SingleOrDefaultAsync(x => x.Id == itemId, cancellationToken) ?? throw new CatalogNotFoundException();
        var offset = page.Offset();
        var query = db.ItemConversions.AsNoTracking().Where(x => x.ItemId == itemId).OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.Skip(offset).Take(page.PageSize).ToArrayAsync(cancellationToken);
        var rateMap = await LoadRatesAsync(rows.Select(x => x.RateId), cancellationToken);
        return new(rows.Select(x => ConversionEntry(x, rateMap.GetValueOrDefault(x.RateId ?? Guid.Empty))).ToArray(), total, page.Page, page.PageSize);
    }

    public async Task<ItemConversionEntry> SaveConversionAsync(Guid? id, ItemConversionInput input, CancellationToken cancellationToken = default)
    {
        await DemandAsync(true, cancellationToken);
        var item = await db.TravelItems.AsNoTracking().SingleOrDefaultAsync(x => x.Id == input.ItemId, cancellationToken) ?? throw new CatalogNotFoundException();
        var destination = input.DestinationCurrency?.Trim().ToUpperInvariant();
        CatalogValidation.Currency(destination);
        var mode = string.IsNullOrWhiteSpace(input.Mode) ? "Calculated" : input.Mode.Trim();
        decimal? reported = input.ReportedAmount;
        ExchangeRate? appliedRate = null;
        if (input.RateId is Guid rateId)
        {
            appliedRate = await db.ExchangeRates.AsNoTracking().SingleOrDefaultAsync(x => x.Id == rateId, cancellationToken) ?? throw new CatalogNotFoundException();
            if (string.Equals(mode, "Calculated", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(appliedRate.SourceCurrency, item.Currency, StringComparison.OrdinalIgnoreCase) || !string.Equals(appliedRate.DestinationCurrency, destination, StringComparison.OrdinalIgnoreCase))
                    throw new CatalogValidationException("A cotação informada não corresponde às moedas do item e da conversão.");
                var origin = (item.BaseAmount ?? 0m) + (item.Fees ?? 0m);
                reported = decimal.Round(origin * appliedRate.Rate, 2, MidpointRounding.AwayFromZero);
            }
        }
        CatalogValidation.Money(reported);
        var entity = id.HasValue ? await db.ItemConversions.SingleOrDefaultAsync(x => x.Id == id.Value, cancellationToken) : null;
        if (id.HasValue)
        {
            if (entity is null) throw new CatalogNotFoundException();
            CatalogValidation.Version(input.Version, entity.Version);
        }
        entity ??= new ItemConversion { TenantId = scope.TenantId!.Value, SpaceId = scope.SpaceId!.Value, CreatedBy = actor.UserId, ItemId = input.ItemId };
        entity.ItemId = input.ItemId;
        entity.RateId = input.RateId;
        entity.DestinationCurrency = destination!;
        entity.Mode = mode;
        entity.ReportedAmount = reported;
        entity.DateContext = string.IsNullOrWhiteSpace(input.DateContext) ? null : input.DateContext.Trim();
        if (!id.HasValue) db.ItemConversions.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ConversionEntry(entity, appliedRate);
    }

    public async Task<TravelBudgetReconciliation> GetBudgetReconciliationAsync(Guid travelId, CancellationToken cancellationToken = default)
    {
        await DemandAsync(false, cancellationToken);
        _ = await db.TravelBudgets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == travelId, cancellationToken) ?? throw new CatalogNotFoundException();
        var items = await db.TravelItems.AsNoTracking().Where(x => x.TravelId == travelId).ToArrayAsync(cancellationToken);
        var itemIds = items.Select(x => x.Id).ToArray();
        var conversions = itemIds.Length == 0
            ? []
            : await db.ItemConversions.AsNoTracking().Where(x => itemIds.Contains(x.ItemId)).ToArrayAsync(cancellationToken);
        var rateMap = await LoadRatesAsync(conversions.Select(x => x.RateId), cancellationToken);
        var discrepancies = new List<TravelDiscrepancy>();

        foreach (var conversion in conversions)
        {
            if (!rateMap.TryGetValue(conversion.RateId ?? Guid.Empty, out var rate) || conversion.ReportedAmount is null) continue;
            var item = items.FirstOrDefault(x => x.Id == conversion.ItemId);
            if (item is null || !string.Equals(rate.SourceCurrency, item.Currency, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(rate.DestinationCurrency, conversion.DestinationCurrency, StringComparison.OrdinalIgnoreCase)) continue;
            var origin = (item.BaseAmount ?? 0m) + (item.Fees ?? 0m);
            var expected = decimal.Round(origin * rate.Rate, 2, MidpointRounding.AwayFromZero);
            if (expected != conversion.ReportedAmount.Value)
            {
                discrepancies.Add(new("SourceTotalMismatch",
                    "O valor informado/calculado da conversão difere do total esperado pela cotação aplicada.",
                    Money(conversion.ReportedAmount.Value - expected), conversion.ItemId, conversion.Id));
            }
        }

        var obligationIds = items.Select(x => x.ObligationId).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        decimal netBalance = 0m;
        if (obligationIds.Length > 0)
        {
            var titleIds = await db.PayableTitles.AsNoTracking().Where(x => obligationIds.Contains(x.ObligationId))
                .Select(x => x.Id).ToArrayAsync(cancellationToken);
            var titles = await db.PayableTitles.AsNoTracking().Where(x => titleIds.Contains(x.Id)).ToArrayAsync(cancellationToken);
            var amounts = await AmountsAsync(titleIds, cancellationToken);
            var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
            foreach (var obligationId in obligationIds)
            {
                var obligationTitles = titles.Where(x => x.ObligationId == obligationId).ToArray();
                if (obligationTitles.Length == 0) continue;
                var obligationBalance = 0m;
                var obligationExcess = 0m;
                foreach (var title in obligationTitles)
                {
                    var a = amounts.GetValueOrDefault(title.Id, TitleAmounts.Zero);
                    var b = TitleBalance.Calculate(title.OriginalAmount, a.Adjustments, a.Allocations, a.Settlements, title.DueDate, asOf);
                    obligationBalance += b.Balance;
                    obligationExcess += b.ExcessCredit;
                }
                netBalance += obligationBalance;
                if (obligationExcess > 0m || obligationBalance < 0m)
                {
                    var negative = obligationBalance < 0m ? obligationBalance : -obligationExcess;
                    discrepancies.Add(new("NegativeTravelBalance",
                        "O saldo da viagem apresenta crédito excedente ou valor negativo para revisão.",
                        Money(negative), ObligationId: obligationId));
                }
            }
        }

        if (netBalance < 0m && !discrepancies.Any(x => x.Kind == "NegativeTravelBalance" && x.ObligationId is null))
            discrepancies.Add(new("NegativeTravelBalance", "O saldo calculado da viagem está negativo e requer revisão.", Money(netBalance)));

        return new(travelId, Money(netBalance), discrepancies);
    }

    private sealed record TitleAmounts(decimal Adjustments, decimal Allocations, decimal Settlements)
    {
        public static readonly TitleAmounts Zero = new(0m, 0m, 0m);
    }

    private async Task<Dictionary<Guid, TitleAmounts>> AmountsAsync(Guid[] titleIds, CancellationToken cancellationToken)
    {
        if (titleIds.Length == 0) return [];
        var adjustments = await db.TitleAdjustments.AsNoTracking()
            .Where(x => titleIds.Contains(x.TitleId) && x.RevokedAt == null)
            .GroupBy(x => x.TitleId)
            .Select(g => new { TitleId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.TitleId, x => x.Total, cancellationToken);
        var allocations = await (from allocation in db.PaymentAllocations.AsNoTracking()
                                 join payment in db.Payments on allocation.PaymentId equals payment.Id
                                 where titleIds.Contains(allocation.TitleId) && payment.State == PaymentState.Confirmed
                                 group allocation by allocation.TitleId into g
                                 select new { TitleId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.TitleId, x => x.Total, cancellationToken);
        var settlements = await db.HistoricalSettlements.AsNoTracking()
            .Where(x => titleIds.Contains(x.TitleId) && x.RevokedAt == null)
            .GroupBy(x => x.TitleId)
            .Select(g => new { TitleId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.TitleId, x => x.Total, cancellationToken);
        var discounts = await (from line in db.AgreementTitles.AsNoTracking()
                               join agreement in db.SettlementAgreements on line.AgreementId equals agreement.Id
                               where titleIds.Contains(line.TitleId) && agreement.State == "Confirmed" && line.Discount > 0
                               group line by line.TitleId into g
                               select new { TitleId = g.Key, Total = g.Sum(x => x.Discount) })
            .ToDictionaryAsync(x => x.TitleId, x => x.Total, cancellationToken);
        return titleIds.Distinct().ToDictionary(
            id => id,
            id => new TitleAmounts(
                adjustments.GetValueOrDefault(id) - discounts.GetValueOrDefault(id),
                allocations.GetValueOrDefault(id),
                settlements.GetValueOrDefault(id)));
    }


    private async Task DemandAsync(bool write, CancellationToken cancellationToken)
    {
        if (!scope.IsAuthorized || actor.UserId is null || !await permissions.HasAsync(write ? "cadastros.editar" : "cadastros.visualizar", cancellationToken))
            throw new AccessDeniedException("Permissão financeira necessária no espaço ativo.");
    }

    private static TravelBudgetEntry BudgetEntry(TravelBudget x) => new(x.Id, x.Name, x.StartDate, x.EndDate, x.Version);
    private static TravelItemEntry ItemEntry(TravelItem x) => new(x.Id, x.TravelId, x.ObligationId, x.CategoryId, x.ResponsibleId, x.Description, x.State, x.Currency, Money(x.BaseAmount), Money(x.Fees), x.Version);
    private static ExchangeRateEntry RateEntry(ExchangeRate x) => new(x.Id, x.SourceCurrency, x.DestinationCurrency, x.RateDate, x.DateContext, x.Rate.ToString(CultureInfo.InvariantCulture), x.Source, x.Version);
    private static ItemConversionEntry ConversionEntry(ItemConversion x, ExchangeRate? rate) => new(
        x.Id, x.ItemId, x.RateId, x.DestinationCurrency, x.Mode, Money(x.ReportedAmount), x.DateContext,
        rate is null ? null : rate.Rate.ToString(CultureInfo.InvariantCulture), rate?.RateDate, rate?.Source, x.Version);

    private async Task<Dictionary<Guid, ExchangeRate>> LoadRatesAsync(IEnumerable<Guid?> rateIds, CancellationToken cancellationToken)
    {
        var ids = rateIds.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        if (ids.Length == 0) return [];
        return await db.ExchangeRates.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
    }
    private static string? Money(decimal? value) => value?.ToString("0.00", CultureInfo.InvariantCulture);
}
