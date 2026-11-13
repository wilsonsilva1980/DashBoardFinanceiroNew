using System.Globalization;
using System.Text.Json;
using Financeiro.Application.Access;
using Financeiro.Application.Catalogs;
using Financeiro.Application.Obligations;
using Financeiro.Domain;
using Financeiro.Domain.Entities;
using Financeiro.Infrastructure.Access;
using Financeiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Infrastructure.Obligations;

public sealed class ObligationService(FinanceiroDbContext db, FinancialScope scope, IRequestActor actor, PermissionEvaluator permissions) : IObligationService
{
    public async Task<PageResult<ObligationEntry>> ListAsync(PageRequest page, CancellationToken cancellationToken = default)
    {
        await DemandAsync(false, cancellationToken);
        var offset = page.Offset();
        var query = db.Obligations.AsNoTracking();
        if (!page.IncludeInactive) query = query.Where(x => x.Active);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip(offset).Take(page.PageSize).ToArrayAsync(cancellationToken);
        return new(rows.Select(Entry).ToArray(), total, page.Page, page.PageSize);
    }

    public async Task<ObligationEntry> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await DemandAsync(false, cancellationToken);
        return Entry(await ObligationAsync(id, cancellationToken));
    }

    public async Task<ObligationEntry> SaveAsync(Guid? id, ObligationInput input, CancellationToken cancellationToken = default)
    {
        await DemandAsync(true, cancellationToken);
        Validate(input);
        var entity = id.HasValue ? await ObligationAsync(id.Value, cancellationToken) : New<Obligation>();
        if (id.HasValue) CatalogValidation.Version(input.Version, entity.Version);
        if ((input.CategoryId.HasValue && !await db.Categorys.AnyAsync(x => x.Id == input.CategoryId, cancellationToken)) ||
            (input.ResponsibleId.HasValue && !await db.Persons.AnyAsync(x => x.Id == input.ResponsibleId, cancellationToken)))
            throw new CatalogValidationException("Categoria ou responsável indisponível no espaço ativo.");

        entity.CategoryId = input.CategoryId;
        entity.ResponsibleId = input.ResponsibleId;
        entity.Description = input.Description.Trim();
        entity.Creditor = TrimToNull(input.Creditor);
        entity.Kind = input.Kind.Trim();
        entity.PurchaseDate = input.PurchaseDate;
        entity.PurchaseDateContext = TrimToNull(input.PurchaseDateContext);
        entity.DueDay = input.DueDay;
        entity.InstallmentCount = input.InstallmentCount;
        entity.Currency = input.Currency;
        entity.Active = input.Active;
        if (!id.HasValue) db.Obligations.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Entry(entity);
    }

    public async Task<PageResult<PayableTitleEntry>> ListTitlesAsync(PageRequest page, Guid? obligationId = null, bool includeForecast = false, DateOnly? asOf = null, Guid? cardId = null, CancellationToken cancellationToken = default)
    {
        await DemandAsync(false, cancellationToken);
        var offset = page.Offset();
        var query = db.PayableTitles.AsNoTracking().Where(x => obligationId == null || x.ObligationId == obligationId);
        if (cardId is not null) query = query.Where(x => x.CardId == cardId);
        if (!includeForecast) query = query.Where(x => x.State != TitleState.Forecast);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(x => x.DueDate == null).ThenBy(x => x.DueDate).ThenBy(x => x.Id).Skip(offset).Take(page.PageSize).ToArrayAsync(cancellationToken);
        var amounts = await AmountsAsync(rows.Select(x => x.Id).ToArray(), cancellationToken);
        var today = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return new(rows.Select(x => Title(x, amounts.GetValueOrDefault(x.Id, TitleAmounts.Zero), today)).ToArray(), total, page.Page, page.PageSize);
    }

    public async Task<PayableTitleEntry> GetTitleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await DemandAsync(false, cancellationToken);
        return await TitleEntryAsync(await TitleAsync(id, cancellationToken), cancellationToken);
    }

    public async Task<PayableTitleEntry> SaveTitleAsync(Guid? id, PayableTitleInput input, CancellationToken cancellationToken = default)
    {
        await DemandAsync(true, cancellationToken);
        Validate(input);
        var obligation = await ObligationAsync(input.ObligationId, cancellationToken);
        var isNew = !id.HasValue;
        var entity = id.HasValue ? await TitleAsync(id.Value, cancellationToken) : New<PayableTitle>();
        if (id.HasValue) CatalogValidation.Version(input.Version, entity.Version);
        if (id.HasValue && entity.ObligationId != obligation.Id) throw new CatalogValidationException("O título não pode ser transferido para outra obrigação.");
        if ((input.ResponsibleId.HasValue && !await db.Persons.AnyAsync(x => x.Id == input.ResponsibleId, cancellationToken)) ||
            (input.CardId.HasValue && !await db.CreditCards.AnyAsync(x => x.Id == input.CardId, cancellationToken)))
            throw new CatalogValidationException("Responsável ou cartão indisponível no espaço ativo.");
        if (input.InstallmentNumber.HasValue && obligation.InstallmentCount is { } count && input.InstallmentNumber > count)
            throw new CatalogValidationException("A parcela deve pertencer à quantidade de parcelas da obrigação.");
        if (await db.PayableTitles.AnyAsync(x => x.ObligationId == obligation.Id && x.OccurrenceKey == input.OccurrenceKey.Trim() && x.Id != entity.Id, cancellationToken))
            throw new CatalogValidationException("Já existe um título com esta chave de ocorrência.");
        if (input.InstallmentNumber.HasValue && await db.PayableTitles.AnyAsync(x => x.ObligationId == obligation.Id && x.InstallmentNumber == input.InstallmentNumber && x.Id != entity.Id, cancellationToken))
            throw new CatalogValidationException("Já existe um título para esta parcela.");
        if (id.HasValue && !entity.CanTransitionTo(input.State))
            throw new CatalogValidationException("A transição do estado do título não é permitida.");

        var previousState = entity.State;
        entity.ObligationId = obligation.Id;
        entity.ResponsibleId = input.ResponsibleId;
        entity.CardId = input.CardId;
        entity.Period = input.Period;
        entity.DueDate = input.DueDate;
        entity.InstallmentNumber = input.InstallmentNumber;
        entity.OccurrenceKey = input.OccurrenceKey.Trim();
        entity.OriginalAmount = input.OriginalAmount;
        entity.State = input.State;
        entity.CancelledAt = input.State == TitleState.Cancelled ? entity.CancelledAt ?? DateTimeOffset.UtcNow : null;
        if (id.HasValue && input.State == TitleState.Cancelled && previousState != TitleState.Cancelled)
            Audit("titulo.cancelado", "PayableTitle", entity.Id, JsonSerializer.Serialize(new { previousState = previousState.ToString(), occurrenceKey = entity.OccurrenceKey, originalAmount = entity.OriginalAmount }));
        if (isNew) db.PayableTitles.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return await TitleEntryAsync(entity, cancellationToken);
    }

    public async Task<PlanningEntryEntry> SavePlanningEntryAsync(Guid titleId, PlanningEntryInput input, CancellationToken cancellationToken = default)
    {
        await DemandAsync(true, cancellationToken);
        CatalogValidation.Money(input.Amount);
        await TitleAsync(titleId, cancellationToken);
        if (!await db.Persons.AnyAsync(x => x.Id == input.PersonId, cancellationToken))
            throw new CatalogValidationException("Responsável indisponível no espaço ativo.");
        var entry = await db.PlanningEntrys.SingleOrDefaultAsync(x => x.TitleId == titleId && x.PersonId == input.PersonId, cancellationToken);
        var isNew = entry is null;
        entry ??= New<PlanningEntry>();
        entry.TitleId = titleId;
        entry.PersonId = input.PersonId;
        entry.Amount = input.Amount;
        if (isNew) db.PlanningEntrys.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        return new(entry.Id, entry.TitleId, entry.PersonId, Money(entry.Amount), entry.Version);
    }

    public async Task<IReadOnlyList<PlanningEntryEntry>> PlanningEntriesAsync(Guid titleId, CancellationToken cancellationToken = default)
    {
        await DemandAsync(false, cancellationToken);
        await TitleAsync(titleId, cancellationToken);
        var rows = await db.PlanningEntrys.AsNoTracking().Where(x => x.TitleId == titleId).OrderBy(x => x.Id).ToArrayAsync(cancellationToken);
        return rows.Select(x => new PlanningEntryEntry(x.Id, x.TitleId, x.PersonId, Money(x.Amount), x.Version)).ToArray();
    }

    public async Task<PageResult<CardInvoiceEntry>> InvoicesAsync(PageRequest page, Guid? cardId, CancellationToken cancellationToken = default)
    {
        await DemandAsync(false, cancellationToken);
        var offset = page.Offset();
        var query = cardId is null ? db.CardInvoices.AsNoTracking() : db.CardInvoices.AsNoTracking().Where(x => x.CardId == cardId.Value);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.Period).ThenBy(x => x.Id).Skip(offset).Take(page.PageSize).ToArrayAsync(cancellationToken);
        return new(rows.Select(Entry).ToArray(), total, page.Page, page.PageSize);
    }

    public async Task<CardInvoiceEntry> GetInvoiceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await DemandAsync(false, cancellationToken);
        var invoice = await db.CardInvoices.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new CatalogNotFoundException();
        return Entry(invoice);
    }

    public async Task<CardInvoiceEntry> SaveInvoiceAsync(Guid? id, CardInvoiceInput input, CancellationToken cancellationToken = default)
    {
        await DemandAsync(true, cancellationToken);
        var invoice = id.HasValue ? await db.CardInvoices.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id.Value, cancellationToken) : null;
        if (id.HasValue && invoice is not null) CatalogValidation.Version(input.Version, invoice.Version);
        var title = await db.PayableTitles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == input.TitleId, cancellationToken) ?? throw new CatalogNotFoundException();
        if (title.CardId != input.CardId) throw new CatalogValidationException("A fatura deve pertencer ao cartão informado.");
        CatalogValidation.Money(input.ReportedAmount);
        if (input.Mode.Length > 50) throw new CatalogValidationException("O modo da fatura excedeu o tamanho permitido.");
        if (invoice is null)
        {
            db.CardInvoices.Add(new CardInvoice
            {
                TenantId = scope.TenantId!.Value,
                SpaceId = scope.SpaceId!.Value,
                CreatedBy = actor.UserId,
                CardId = input.CardId,
                TitleId = input.TitleId,
                Period = input.Period,
                Mode = input.Mode.Trim(),
                ReportedAmount = input.ReportedAmount,
                Notes = input.Notes
            });
        }
        else
        {
            invoice.CardId = input.CardId;
            invoice.TitleId = input.TitleId;
            invoice.Period = input.Period;
            invoice.Mode = input.Mode.Trim();
            invoice.ReportedAmount = input.ReportedAmount;
            invoice.Notes = input.Notes;
        }
        await db.SaveChangesAsync(cancellationToken);
        var saved = invoice ?? await db.CardInvoices.AsNoTracking().SingleAsync(x => x.CardId == input.CardId && x.TitleId == input.TitleId, cancellationToken);
        return Entry(saved);
    }

    public async Task<InvoicePurchaseEntry> SavePurchaseAsync(Guid? id, InvoicePurchaseInput input, CancellationToken cancellationToken = default)
    {
        await DemandAsync(true, cancellationToken);
        var invoice = await db.CardInvoices.AsNoTracking().SingleOrDefaultAsync(x => x.Id == input.InvoiceId, cancellationToken) ?? throw new CatalogNotFoundException();
        var purchase = id.HasValue ? await db.InvoicePurchases.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id.Value, cancellationToken) : null;
        if (id.HasValue && purchase is not null) CatalogValidation.Version(input.Version, purchase.Version);
        CatalogValidation.Money(input.Amount);
        if (purchase is null)
        {
            db.InvoicePurchases.Add(new InvoicePurchase
            {
                TenantId = scope.TenantId!.Value,
                SpaceId = scope.SpaceId!.Value,
                CreatedBy = actor.UserId,
                InvoiceId = input.InvoiceId,
                PurchaseObligationId = input.PurchaseObligationId,
                Description = input.Description,
                Amount = input.Amount,
                InstallmentNumber = input.InstallmentNumber,
                OccurrenceKey = input.OccurrenceKey
            });
        }
        else
        {
            purchase.InvoiceId = input.InvoiceId;
            purchase.PurchaseObligationId = input.PurchaseObligationId;
            purchase.Description = input.Description;
            purchase.Amount = input.Amount;
            purchase.InstallmentNumber = input.InstallmentNumber;
            purchase.OccurrenceKey = input.OccurrenceKey;
        }
        await db.SaveChangesAsync(cancellationToken);
        return new(purchase!.Id, purchase.InvoiceId, purchase.PurchaseObligationId, purchase.Description, purchase.Amount, purchase.InstallmentNumber, purchase.OccurrenceKey, purchase.Version);
    }

    private static CardInvoiceEntry Entry(CardInvoice x) => new(x.Id, x.CardId, x.TitleId, x.Period, x.Mode, x.ReportedAmount?.ToString("0.00", CultureInfo.InvariantCulture), x.Notes, x.Version);

    public async Task DeleteAsync(Guid id, long version, CancellationToken cancellationToken = default)
    {
        await DemandAsync(true, cancellationToken);
        var entity = await ObligationAsync(id, cancellationToken);
        if (entity.Version != version) throw new RequestConflictException("A obrigação foi alterada. Recarregue antes de excluir.");
        if (await db.PayableTitles.AnyAsync(x => x.ObligationId == id, cancellationToken))
            throw new RequestConflictException("A obrigação possui títulos vinculados. Desative-a e cancele os títulos para preservar o histórico financeiro.");
        db.Obligations.Remove(entity);
        Audit("obrigacao.excluida", "Obligation", id, JsonSerializer.Serialize(new { description = entity.Description, kind = entity.Kind, installmentCount = entity.InstallmentCount }));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteTitleAsync(Guid id, long version, CancellationToken cancellationToken = default)
    {
        await DemandAsync(true, cancellationToken);
        var entity = await TitleAsync(id, cancellationToken);
        if (entity.Version != version) throw new RequestConflictException("O título foi alterado. Recarregue antes de excluir.");
        if (entity.State != TitleState.Draft)
            throw new RequestConflictException("Somente títulos em rascunho podem ser excluídos. Cancele o título para preservar o histórico financeiro.");
        if (await db.PaymentAllocations.AnyAsync(x => x.TitleId == id, cancellationToken) ||
            await db.HistoricalSettlements.AnyAsync(x => x.TitleId == id, cancellationToken) ||
            await db.TitleAdjustments.AnyAsync(x => x.TitleId == id, cancellationToken) ||
            await db.AgreementTitles.AnyAsync(x => x.TitleId == id, cancellationToken) ||
            await db.ScenarioTitles.AnyAsync(x => x.TitleId == id, cancellationToken) ||
            await db.PlanningEntrys.AnyAsync(x => x.TitleId == id, cancellationToken))
            throw new RequestConflictException("O título possui referências financeiras ou de planejamento e não pode ser excluído. Cancele-o para preservar o histórico.");
        db.PayableTitles.Remove(entity);
        Audit("titulo.excluido", "PayableTitle", id, JsonSerializer.Serialize(new { occurrenceKey = entity.OccurrenceKey, originalAmount = entity.OriginalAmount, state = entity.State.ToString() }));
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task DemandAsync(bool write, CancellationToken cancellationToken)
    {
        if (!scope.IsAuthorized || actor.UserId is null || !await permissions.HasAsync(write ? "cadastros.editar" : "cadastros.visualizar", cancellationToken))
            throw new AccessDeniedException("Permissão financeira necessária no espaço ativo.");
    }

    private T New<T>() where T : ScopedEntity, new() => new() { TenantId = scope.TenantId!.Value, SpaceId = scope.SpaceId!.Value, CreatedBy = actor.UserId };

    private void Audit(string action, string entity, Guid entityId, string changesJson) => db.AuditRecords.Add(new AuditRecord
    {
        TenantId = scope.TenantId!.Value,
        SpaceId = scope.SpaceId!.Value,
        UserId = actor.UserId,
        CreatedBy = actor.UserId,
        Action = action,
        Entity = entity,
        EntityId = entityId,
        Instant = DateTimeOffset.UtcNow,
        CorrelationId = actor.CorrelationId,
        ChangesJson = changesJson
    });
    private async Task<Obligation> ObligationAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Obligations.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new CatalogNotFoundException();
    private async Task<PayableTitle> TitleAsync(Guid id, CancellationToken cancellationToken) =>
        await db.PayableTitles.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new CatalogNotFoundException();
    private static string? TrimToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Money(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);
    private static ObligationEntry Entry(Obligation x) => new(x.Id, x.CategoryId, x.ResponsibleId, x.Description, x.Creditor, x.Kind, x.PurchaseDate, x.PurchaseDateContext, x.DueDay, x.InstallmentCount, x.Currency, x.Active, x.Version);

    private sealed record TitleAmounts(decimal Adjustments, decimal Allocations, decimal Settlements)
    {
        public static readonly TitleAmounts Zero = new(0m, 0m, 0m);
    }

    private async Task<PayableTitleEntry> TitleEntryAsync(PayableTitle title, CancellationToken cancellationToken)
    {
        var amounts = (await AmountsAsync([title.Id], cancellationToken)).GetValueOrDefault(title.Id, TitleAmounts.Zero);
        return Title(title, amounts, DateOnly.FromDateTime(DateTime.UtcNow));
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
        // Confirmed agreements carry explicit per-title discounts; simulations never reach this view.
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

    private static PayableTitleEntry Title(PayableTitle x, TitleAmounts amounts, DateOnly asOf)
    {
        var balance = TitleBalance.Calculate(x.OriginalAmount, amounts.Adjustments, amounts.Allocations, amounts.Settlements, x.DueDate, asOf);
        return new(x.Id, x.ObligationId, x.ResponsibleId, x.CardId, x.Period, x.DueDate, x.InstallmentNumber, x.OccurrenceKey,
            Money(x.OriginalAmount), x.State, x.CancelledAt, x.Version,
            Money(balance.Balance), Money(balance.SettledAmount), Money(balance.ExcessCredit), balance.Status);
    }

    private static void Validate(ObligationInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Description) || input.Description.Trim().Length > 1000 || string.IsNullOrWhiteSpace(input.Kind) || input.Kind.Trim().Length > 50)
            throw new CatalogValidationException("Informe descrição e tipo válidos.");
        if (input.Creditor?.Length > 200 || input.PurchaseDateContext?.Length > 200) throw new CatalogValidationException("Um campo de contexto excedeu o tamanho permitido.");
        CatalogValidation.Currency(input.Currency);
        CatalogValidation.Day(input.DueDay);
        if (input.InstallmentCount is <= 0 or > 999) throw new CatalogValidationException("Informe uma quantidade de parcelas entre 1 e 999.");
    }

    private static void Validate(PayableTitleInput input)
    {
        if (input.Period.Day != 1) throw new CatalogValidationException("O período do título deve iniciar no primeiro dia do mês.");
        if (string.IsNullOrWhiteSpace(input.OccurrenceKey) || input.OccurrenceKey.Trim().Length > 200) throw new CatalogValidationException("Informe uma chave de ocorrência válida.");
        CatalogValidation.Money(input.OriginalAmount);
        if (input.InstallmentNumber is <= 0) throw new CatalogValidationException("A parcela deve ser positiva.");
    }
}
