using System.Globalization;
using System.Text.Json;
using Financeiro.Application.Access;
using Financeiro.Application.Catalogs;
using Financeiro.Application.Recurrences;
using Financeiro.Domain;
using Financeiro.Domain.Entities;
using Financeiro.Infrastructure.Access;
using Financeiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Infrastructure.Recurrences;

public sealed class RecurrenceService(FinanceiroDbContext db, FinancialScope scope, IRequestActor actor, PermissionEvaluator permissions) : IRecurrenceService
{
    public async Task<PageResult<RecurrenceEntry>> ListAsync(PageRequest page, CancellationToken cancellationToken = default)
    {
        await DemandAsync(false, cancellationToken);
        var offset = page.Offset();
        var query = db.Recurrences.AsNoTracking().OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.Skip(offset).Take(page.PageSize).ToArrayAsync(cancellationToken);
        return new(rows.Select(Entry).ToArray(), total, page.Page, page.PageSize);
    }

    public async Task<RecurrenceEntry> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await DemandAsync(false, cancellationToken);
        var entity = await db.Recurrences.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new CatalogNotFoundException();
        return Entry(entity);
    }

    public async Task<RecurrenceEntry> SaveAsync(Guid? id, RecurrenceInput input, CancellationToken cancellationToken = default)
    {
        await DemandAsync(true, cancellationToken);
        var entity = id.HasValue ? await db.Recurrences.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id.Value, cancellationToken) : null;
        if (id.HasValue && entity is not null) CatalogValidation.Version(input.Version, entity.Version);
        var obligation = await db.Obligations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == input.ObligationId, cancellationToken) ?? throw new CatalogNotFoundException();
        if (input.EndDate is { } endDate && endDate < input.AnchorDate) throw new CatalogValidationException("A data final deve ser posterior ou igual à data de ancoragem.");
        CatalogValidation.Day(input.NominalDay);

        if (entity is null)
        {
            db.Recurrences.Add(new Recurrence
            {
                TenantId = scope.TenantId!.Value,
                SpaceId = scope.SpaceId!.Value,
                CreatedBy = actor.UserId,
                ObligationId = input.ObligationId,
                AnchorDate = input.AnchorDate,
                NominalDay = input.NominalDay,
                EndDate = input.EndDate,
                Active = input.Active
            });
        }
        else
        {
            entity.ObligationId = input.ObligationId;
            entity.AnchorDate = input.AnchorDate;
            entity.NominalDay = input.NominalDay;
            entity.EndDate = input.EndDate;
            entity.Active = input.Active;
        }
        await db.SaveChangesAsync(cancellationToken);
        return Entry(await db.Recurrences.AsNoTracking().SingleAsync(x => x.Id == (id ?? entity!.Id), cancellationToken));
    }

    public async Task<RecurrencePriceEntry> SavePriceAsync(Guid recurrenceId, RecurrencePriceInput input, CancellationToken cancellationToken = default)
    {
        await DemandAsync(true, cancellationToken);
        var recurrence = await db.Recurrences.AsNoTracking().SingleOrDefaultAsync(x => x.Id == recurrenceId, cancellationToken) ?? throw new CatalogNotFoundException();
        CatalogValidation.Money(input.CycleAmount);
        CatalogValidation.Currency(input.Currency);
        if (input.IntervalMonths is < 1 or > 120) throw new CatalogValidationException("O intervalo de meses deve estar entre 1 e 120.");
        var price = new RecurrencePrice
        {
            TenantId = scope.TenantId!.Value,
            SpaceId = scope.SpaceId!.Value,
            CreatedBy = actor.UserId,
            RecurrenceId = recurrenceId,
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            CycleAmount = input.CycleAmount,
            Currency = input.Currency,
            IntervalMonths = input.IntervalMonths
        };
        db.RecurrencePrices.Add(price);
        await db.SaveChangesAsync(cancellationToken);
        return new(price.Id, price.RecurrenceId, price.StartDate, price.EndDate, price.CycleAmount, price.Currency, price.IntervalMonths);
    }

    public async Task<RecurrenceGenerationResult> GenerateHorizonAsync(Guid recurrenceId, RecurrenceGenerationInput input, CancellationToken cancellationToken = default)
    {
        await DemandAsync(true, cancellationToken);
        var recurrence = await db.Recurrences.AsNoTracking().SingleOrDefaultAsync(x => x.Id == recurrenceId, cancellationToken) ?? throw new CatalogNotFoundException();
        if (!recurrence.Active) return new(0, 0);
        var obligation = await db.Obligations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == recurrence.ObligationId, cancellationToken) ?? throw new CatalogNotFoundException();
        var prices = await db.RecurrencePrices.AsNoTracking().Where(x => x.RecurrenceId == recurrenceId).OrderBy(x => x.StartDate).ToArrayAsync(cancellationToken);
        var anchor = recurrence.AnchorDate ?? input.From;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var limit = recurrence.EndDate ?? input.To;
        var created = 0;
        var skipped = 0;
        foreach (var price in prices.Where(p => p.StartDate <= limit && (p.EndDate ?? limit) >= input.From).OrderBy(p => p.StartDate))
        {
            var from = input.From > price.StartDate ? input.From : price.StartDate;
            var to = (price.EndDate ?? limit) < limit ? price.EndDate!.Value : limit;
            foreach (var occurrence in RecurrenceCalendar.Plan(anchor, recurrence.NominalDay, price.IntervalMonths, from, to, price.CycleAmount, price.Currency))
            {
                var key = RecurrenceCalendar.OccurrenceKey(recurrenceId, occurrence.DueDate.Year, occurrence.DueDate.Month);
                if (await db.PayableTitles.AnyAsync(x => x.OccurrenceKey == key, cancellationToken)) { skipped++; continue; }
                db.PayableTitles.Add(new PayableTitle
                {
                    TenantId = scope.TenantId!.Value,
                    SpaceId = scope.SpaceId!.Value,
                    CreatedBy = actor.UserId,
                    ObligationId = obligation.Id,
                    ResponsibleId = obligation.ResponsibleId,
                    Period = occurrence.PeriodStart,
                    DueDate = occurrence.DueDate,
                    OccurrenceKey = key,
                    OriginalAmount = occurrence.CycleAmount,
                    State = occurrence.DueDate < today ? TitleState.Effective : TitleState.Forecast
                });
                created++;
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        return new(created, skipped);
    }

    private async Task DemandAsync(bool write, CancellationToken cancellationToken)
    {
        if (!scope.IsAuthorized || actor.UserId is null || !await permissions.HasAsync(write ? "cadastros.editar" : "cadastros.visualizar", cancellationToken))
            throw new AccessDeniedException("Permissão financeira necessária no espaço ativo.");
    }

    private static RecurrenceEntry Entry(Recurrence x) => new(x.Id, x.ObligationId, x.AnchorDate ?? DateOnly.FromDateTime(DateTime.UtcNow), x.NominalDay, x.EndDate, x.Active, x.Version);
}
