using System.Globalization;
using Financeiro.Application.Access;
using Financeiro.Application.Catalogs;
using Financeiro.Application.Dashboard;
using Financeiro.Domain;
using Financeiro.Domain.Entities;
using Financeiro.Infrastructure.Access;
using Financeiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Infrastructure.Dashboard;

public sealed class DashboardQueries(FinanceiroDbContext db, PermissionEvaluator permissions) : IDashboardQueries
{
    public async Task<CashFlowSummary> CashFlowAsync(DateOnly from, DateOnly until, string currency, CancellationToken cancellationToken = default)
    {
        await DemandAsync(cancellationToken);
        ValidatePeriod(from, until, currency);
        var (paid, historic) = await CashFlowTotalsAsync(from, until, currency, cancellationToken);
        return new(from, until, currency, Money(paid + historic), null);
    }

    public async Task<DashboardSummary> SummaryAsync(DateOnly from, DateOnly until, string currency, CancellationToken cancellationToken = default)
    {
        await DemandAsync(cancellationToken);
        ValidatePeriod(from, until, currency);
        var asOf = until.AddDays(-1);

        var baseRows = await db.PayableTitles.AsNoTracking()
            .Join(db.Obligations.AsNoTracking(), title => title.ObligationId, obligation => obligation.Id, (title, obligation) => new { title, obligation })
            .Where(x => x.obligation.Currency == currency
                && x.title.State != TitleState.Cancelled
                && x.title.DueDate != null
                && x.title.DueDate >= from
                && x.title.DueDate < until)
            .Select(x => new
            {
                x.title.Id,
                x.title.ObligationId,
                x.title.OriginalAmount,
                x.title.State,
                x.title.DueDate,
                x.title.Period,
                x.obligation.CategoryId,
                ResponsibleId = x.title.ResponsibleId ?? x.obligation.ResponsibleId
            })
            .ToArrayAsync(cancellationToken);

        var categoryIds = baseRows.Where(x => x.CategoryId.HasValue).Select(x => x.CategoryId!.Value).Distinct().ToArray();
        var responsibleIds = baseRows.Where(x => x.ResponsibleId.HasValue).Select(x => x.ResponsibleId!.Value).Distinct().ToArray();
        var categoryNames = categoryIds.Length == 0 ? new Dictionary<Guid, string>() : await db.Categorys.AsNoTracking().Where(x => categoryIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        var responsibleNames = responsibleIds.Length == 0 ? new Dictionary<Guid, string>() : await db.Persons.AsNoTracking().Where(x => responsibleIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var titleRows = baseRows.Select(x => new TitleRow(
            x.Id, x.ObligationId, x.OriginalAmount, x.State, x.DueDate, x.Period,
            x.CategoryId, x.CategoryId.HasValue && categoryNames.TryGetValue(x.CategoryId.Value, out var cn) ? cn : null,
            x.ResponsibleId, x.ResponsibleId.HasValue && responsibleNames.TryGetValue(x.ResponsibleId.Value, out var rn) ? rn : null
        )).ToArray();

        // Titles without a known due date cannot be placed in a dated summary bucket; keep them out of fabricated periods.
        var amounts = await AmountsAsync(titleRows.Select(x => x.Id).ToArray(), cancellationToken);
        var computed = titleRows.Select(x =>
        {
            var a = amounts.GetValueOrDefault(x.Id, TitleAmounts.Zero);
            var balance = TitleBalance.Calculate(x.OriginalAmount, a.Adjustments, a.Allocations, a.Settlements, x.DueDate, asOf);
            return new ComputedTitle(x, balance);
        }).ToArray();

        var totals = BuildState(computed);
        var monthly = computed
            .GroupBy(x => new DateOnly(x.Row.DueDate!.Value.Year, x.Row.DueDate!.Value.Month, 1))
            .OrderBy(x => x.Key)
            .Select(g => new DashboardMonthlyEvolution(
                g.Key,
                Money(g.Where(x => x.Row.State == TitleState.Effective).Sum(Outstanding)),
                Money(g.Where(x => x.Row.State == TitleState.Forecast).Sum(x => x.Row.OriginalAmount)),
                Money(g.Where(x => x.Balance.Status == SettlementStatus.Open).Sum(Outstanding)),
                Money(g.Sum(x => x.Balance.SettledAmount)),
                Money(g.Where(x => x.Balance.Status == SettlementStatus.Overdue).Sum(Outstanding))))
            .ToArray();

        var byCategory = computed
            .GroupBy(x => x.Row.CategoryId)
            .Select(g => new DashboardCategorySummary(g.Key, g.First().Row.CategoryName ?? "Sem categoria", BuildState(g)))
            .OrderBy(x => x.CategoryName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        var byResponsible = computed
            .GroupBy(x => x.Row.ResponsibleId)
            .Select(g => new DashboardResponsibleSummary(g.Key, g.First().Row.ResponsibleName ?? "Sem responsável", BuildState(g)))
            .OrderBy(x => x.ResponsibleName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        var byDueDate = computed
            .GroupBy(x => x.Row.DueDate)
            .OrderBy(x => x.Key)
            .Select(g => new DashboardDueDateSummary(g.Key, g.Key?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "Sem data", BuildState(g)))
            .ToArray();

        var positions = await LatestPositionsAsync(currency, until, cancellationToken);
        var indicators = await ReconciliationIndicatorsAsync(currency, until, cancellationToken);

        return new(from, until, currency, null, totals, monthly, byCategory, byResponsible, byDueDate, positions, indicators);
    }

    private async Task DemandAsync(CancellationToken cancellationToken)
    {
        if (!await permissions.HasAsync("dashboard.visualizar", cancellationToken))
            throw new AccessDeniedException("Permissão de dashboard necessária.");
    }

    private static void ValidatePeriod(DateOnly from, DateOnly until, string currency)
    {
        CatalogValidation.Currency(currency);
        if (until <= from) throw new CatalogValidationException("O fim do período deve ser posterior ao início.");
    }

    private async Task<(decimal paid, decimal historic)> CashFlowTotalsAsync(DateOnly from, DateOnly until, string currency, CancellationToken cancellationToken)
    {
        var paid = await db.Payments.AsNoTracking()
            .Where(x => x.State == PaymentState.Confirmed && x.Currency == currency && x.PaymentDate >= from && x.PaymentDate < until)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
        var historic = await db.HistoricalSettlements.AsNoTracking()
            .Where(settlement => settlement.RevokedAt == null && settlement.EffectiveDate != null && settlement.EffectiveDate >= from && settlement.EffectiveDate < until)
            .Join(db.PayableTitles.AsNoTracking(), settlement => settlement.TitleId, title => title.Id, (settlement, title) => new { settlement, title })
            .Join(db.Obligations.AsNoTracking(), x => x.title.ObligationId, obligation => obligation.Id, (x, obligation) => new { x.settlement, obligation })
            .Where(x => x.obligation.Currency == currency)
            .SumAsync(x => (decimal?)x.settlement.Amount, cancellationToken) ?? 0m;
        return (paid, historic);
    }

    private async Task<IReadOnlyList<DashboardAccountPositionSummary>> LatestPositionsAsync(string currency, DateOnly until, CancellationToken cancellationToken)
    {
        var accounts = await (
            from account in db.BankAccounts.AsNoTracking()
            join institution in db.Institutions.AsNoTracking() on account.InstitutionId equals institution.Id
            where account.Currency == currency
            select new { account.Id, account.OverdraftLimit, Label = institution.Name + " " + (account.Number ?? "") }
        ).ToArrayAsync(cancellationToken);

        var results = new List<DashboardAccountPositionSummary>(accounts.Length);
        foreach (var account in accounts)
        {
            var snapshot = await db.AccountPositions.AsNoTracking()
                .Where(x => x.AccountId == account.Id && x.PositionDate < until)
                .OrderByDescending(x => x.PositionDate).ThenByDescending(x => x.CreatedAt)
                .Select(x => new { x.PositionDate, x.Balance, x.SavingsBalance, x.OverdraftUsed })
                .FirstOrDefaultAsync(cancellationToken);
            if (snapshot is null) continue;
            var available = snapshot.OverdraftUsed.HasValue ? account.OverdraftLimit - snapshot.OverdraftUsed.Value : (decimal?)null;
            results.Add(new(account.Id, account.Label.Trim(), snapshot.PositionDate, Money(snapshot.Balance), Money(snapshot.SavingsBalance), Money(snapshot.OverdraftUsed), Money(account.OverdraftLimit), Money(available)));
        }
        return results.OrderBy(x => x.AccountLabel, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }

    private async Task<IReadOnlyList<DashboardReconciliationIndicator>> ReconciliationIndicatorsAsync(string currency, DateOnly until, CancellationToken cancellationToken)
    {
        var indicators = new List<DashboardReconciliationIndicator>();
        var asOf = until.AddDays(-1);

        var activeTitleIds = await (
            from title in db.PayableTitles.AsNoTracking()
            join obligation in db.Obligations.AsNoTracking() on title.ObligationId equals obligation.Id
            where obligation.Currency == currency && title.State != TitleState.Cancelled
            select title.Id).ToArrayAsync(cancellationToken);
        var titleRows = await db.PayableTitles.AsNoTracking().Where(x => activeTitleIds.Contains(x.Id))
            .Select(x => new { x.Id, x.OriginalAmount, x.DueDate }).ToArrayAsync(cancellationToken);
        var amounts = await AmountsAsync(activeTitleIds, cancellationToken);
        foreach (var title in titleRows)
        {
            var a = amounts.GetValueOrDefault(title.Id, TitleAmounts.Zero);
            var balance = TitleBalance.Calculate(title.OriginalAmount, a.Adjustments, a.Allocations, a.Settlements, title.DueDate, asOf);
            if (balance.ExcessCredit > 0)
                indicators.Add(new("ExcessCredit", "PayableTitle", "Crédito excedente identificado para revisão; o valor não reduz silenciosamente os totais normais.", Money(balance.ExcessCredit), title.Id));
        }

        var invoiceRows = await (
            from invoice in db.CardInvoices.AsNoTracking()
            join title in db.PayableTitles.AsNoTracking() on invoice.TitleId equals title.Id
            join obligation in db.Obligations.AsNoTracking() on title.ObligationId equals obligation.Id
            where obligation.Currency == currency && invoice.ReportedAmount != null
            select new { invoice.Id, invoice.ReportedAmount, invoice.TitleId }).ToArrayAsync(cancellationToken);
        foreach (var invoice in invoiceRows)
        {
            var purchases = await db.InvoicePurchases.AsNoTracking().Where(x => x.InvoiceId == invoice.Id).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
            if (purchases != invoice.ReportedAmount!.Value)
            {
                indicators.Add(new("SourceTotalMismatch", "CardInvoice",
                    "O total informado da fatura difere da soma das compras analíticas e permanece separado dos totais normais.",
                    Money(invoice.ReportedAmount.Value - purchases), invoice.TitleId));
            }
        }

        var budgetIds = await db.TravelBudgets.AsNoTracking().Select(x => x.Id).ToArrayAsync(cancellationToken);
        foreach (var budgetId in budgetIds)
        {
            var recon = await TravelReconciliationAsync(budgetId, asOf, cancellationToken);
            foreach (var discrepancy in recon.Discrepancies)
                indicators.Add(new(discrepancy.Kind, "TravelBudget", discrepancy.Message, discrepancy.Amount, discrepancy.ItemId ?? discrepancy.ConversionId ?? discrepancy.ObligationId, budgetId));
        }

        return indicators.OrderBy(x => x.Scope).ThenBy(x => x.Kind).ToArray();
    }

    private async Task<TravelReconciliationResult> TravelReconciliationAsync(Guid travelId, DateOnly asOf, CancellationToken cancellationToken)
    {
        var items = await db.TravelItems.AsNoTracking().Where(x => x.TravelId == travelId).ToArrayAsync(cancellationToken);
        var itemIds = items.Select(x => x.Id).ToArray();
        var conversions = itemIds.Length == 0 ? [] : await db.ItemConversions.AsNoTracking().Where(x => itemIds.Contains(x.ItemId)).ToArrayAsync(cancellationToken);
        var rateIds = conversions.Where(x => x.RateId.HasValue).Select(x => x.RateId!.Value).Distinct().ToArray();
        var rateMap = rateIds.Length == 0 ? new Dictionary<Guid, ExchangeRate>() : await db.ExchangeRates.AsNoTracking().Where(x => rateIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
        var discrepancies = new List<DiscrepancyRow>();
        foreach (var conversion in conversions)
        {
            if (!conversion.RateId.HasValue || conversion.ReportedAmount is null || !rateMap.TryGetValue(conversion.RateId.Value, out var rate)) continue;
            var item = items.FirstOrDefault(x => x.Id == conversion.ItemId);
            if (item is null || !string.Equals(rate.SourceCurrency, item.Currency, StringComparison.OrdinalIgnoreCase) || !string.Equals(rate.DestinationCurrency, conversion.DestinationCurrency, StringComparison.OrdinalIgnoreCase)) continue;
            var expected = decimal.Round(((item.BaseAmount ?? 0m) + (item.Fees ?? 0m)) * rate.Rate, 2, MidpointRounding.AwayFromZero);
            if (expected != conversion.ReportedAmount.Value)
                discrepancies.Add(new("SourceTotalMismatch", "O valor informado/calculado da conversão difere do total esperado pela cotação aplicada.", Money(conversion.ReportedAmount.Value - expected), conversion.ItemId, conversion.Id, null));
        }

        var obligationIds = items.Select(x => x.ObligationId).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        decimal net = 0m;
        if (obligationIds.Length > 0)
        {
            var titles = await db.PayableTitles.AsNoTracking().Where(x => obligationIds.Contains(x.ObligationId)).Select(x => new { x.Id, x.ObligationId, x.OriginalAmount, x.DueDate }).ToArrayAsync(cancellationToken);
            var tAmounts = await AmountsAsync(titles.Select(x => x.Id).ToArray(), cancellationToken);
            foreach (var obligationId in obligationIds)
            {
                decimal obligationBalance = 0m;
                decimal obligationExcess = 0m;
                foreach (var title in titles.Where(x => x.ObligationId == obligationId))
                {
                    var a = tAmounts.GetValueOrDefault(title.Id, TitleAmounts.Zero);
                    var b = TitleBalance.Calculate(title.OriginalAmount, a.Adjustments, a.Allocations, a.Settlements, title.DueDate, asOf);
                    obligationBalance += b.Balance;
                    obligationExcess += b.ExcessCredit;
                }
                net += obligationBalance;
                if (obligationExcess > 0m || obligationBalance < 0m)
                    discrepancies.Add(new("NegativeTravelBalance", "O saldo da viagem apresenta crédito excedente ou valor negativo para revisão.", Money(obligationBalance < 0m ? obligationBalance : -obligationExcess), null, null, obligationId));
            }
        }
        if (net < 0m && !discrepancies.Any(x => x.Kind == "NegativeTravelBalance" && x.ObligationId is null))
            discrepancies.Add(new("NegativeTravelBalance", "O saldo calculado da viagem está negativo e requer revisão.", Money(net), null, null, null));
        return new(discrepancies);
    }

    private static DashboardAmountState BuildState(IEnumerable<ComputedTitle> titles)
    {
        var list = titles as ComputedTitle[] ?? titles.ToArray();
        return new(
            Money(list.Where(x => x.Row.State == TitleState.Effective).Sum(Outstanding)),
            Money(list.Where(x => x.Row.State == TitleState.Forecast).Sum(x => x.Row.OriginalAmount)),
            Money(list.Where(x => x.Balance.Status == SettlementStatus.Open).Sum(Outstanding)),
            Money(list.Where(x => x.Balance.Status == SettlementStatus.Partial).Sum(Outstanding)),
            Money(list.Sum(x => x.Balance.SettledAmount)),
            Money(list.Where(x => x.Balance.Status == SettlementStatus.Overdue).Sum(Outstanding)));
    }

    private static decimal Outstanding(ComputedTitle x) => x.Balance.Balance > 0 ? x.Balance.Balance : 0m;

    private sealed record TitleRow(Guid Id, Guid ObligationId, decimal OriginalAmount, TitleState State, DateOnly? DueDate, DateOnly Period, Guid? CategoryId, string? CategoryName, Guid? ResponsibleId, string? ResponsibleName);
    private sealed record ComputedTitle(TitleRow Row, TitleBalance Balance);
    private sealed record TravelReconciliationResult(IReadOnlyList<DiscrepancyRow> Discrepancies);
    private sealed record DiscrepancyRow(string Kind, string Message, string? Amount, Guid? ItemId, Guid? ConversionId, Guid? ObligationId);

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
                                 join payment in db.Payments.AsNoTracking() on allocation.PaymentId equals payment.Id
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
                               join agreement in db.SettlementAgreements.AsNoTracking() on line.AgreementId equals agreement.Id
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

    private static string Money(decimal? value) => (value ?? 0m).ToString("0.00", CultureInfo.InvariantCulture);
}
