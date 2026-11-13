using System.Globalization;
using System.Text.Json;
using Financeiro.Application.Access;
using Financeiro.Application.Catalogs;
using Financeiro.Application.Payments;
using Financeiro.Domain.Entities;
using Financeiro.Infrastructure.Access;
using Financeiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Infrastructure.Payments;

public sealed class HistoricalSettlementService(FinanceiroDbContext db, FinancialScope scope, IRequestActor actor, PermissionEvaluator permissions) : IHistoricalSettlementService
{
    public async Task<IReadOnlyList<HistoricalSettlementEntry>> ListAsync(Guid titleId, CancellationToken cancellationToken = default)
    {
        await DemandAsync(false, cancellationToken);
        await TitleAsync(titleId, cancellationToken);
        var rows = await db.HistoricalSettlements.AsNoTracking()
            .Where(x => x.TitleId == titleId)
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
            .ToArrayAsync(cancellationToken);
        return rows.Select(Entry).ToArray();
    }

    public async Task<HistoricalSettlementEntry> CreateAsync(Guid titleId, HistoricalSettlementInput input, CancellationToken cancellationToken = default)
    {
        await DemandAsync(true, cancellationToken);
        var title = await TitleAsync(titleId, cancellationToken);
        if (title.State == TitleState.Cancelled)
            throw new CatalogValidationException("Títulos cancelados não recebem baixas históricas.");
        CatalogValidation.Money(input.Amount);
        if (input.Amount <= 0) throw new CatalogValidationException("Informe um valor de baixa positivo.");
        if (string.IsNullOrWhiteSpace(input.Justification) || input.Justification.Trim().Length > 1000)
            throw new CatalogValidationException("Informe a justificativa da baixa histórica.");
        CatalogValidation.Text(input.PreviousReference, 200);
        CatalogValidation.Text(input.SourceStatus, 100);
        if (input.PayerId.HasValue && !await db.Persons.AnyAsync(x => x.Id == input.PayerId, cancellationToken))
            throw new CatalogValidationException("Pagador indisponível no espaço ativo.");

        var settlement = new HistoricalSettlement
        {
            TenantId = scope.TenantId!.Value,
            SpaceId = scope.SpaceId!.Value,
            CreatedBy = actor.UserId,
            TitleId = title.Id,
            PayerId = input.PayerId,
            ApprovedBy = actor.UserId ?? throw new AccessDeniedException("Usuário autenticado obrigatório."),
            Justification = input.Justification.Trim(),
            Amount = input.Amount,
            EffectiveDate = input.EffectiveDate,
            PreviousReference = TrimToNull(input.PreviousReference),
            SourceStatus = TrimToNull(input.SourceStatus)
        };
        db.HistoricalSettlements.Add(settlement);
        Audit("liquidacao-historica.registrada", settlement.Id, JsonSerializer.Serialize(new
        {
            titleId = settlement.TitleId,
            amount = settlement.Amount,
            effectiveDate = settlement.EffectiveDate,
            previousReference = settlement.PreviousReference
        }));
        await db.SaveChangesAsync(cancellationToken);
        return Entry(settlement);
    }

    public async Task<HistoricalSettlementEntry> RevokeAsync(Guid titleId, Guid settlementId, HistoricalSettlementRevocationInput input, CancellationToken cancellationToken = default)
    {
        await DemandAsync(true, cancellationToken);
        await TitleAsync(titleId, cancellationToken);
        var settlement = await db.HistoricalSettlements.SingleOrDefaultAsync(x => x.Id == settlementId && x.TitleId == titleId, cancellationToken)
            ?? throw new CatalogNotFoundException();
        CatalogValidation.Version(input.Version, settlement.Version);
        if (settlement.RevokedAt.HasValue)
            throw new RequestConflictException("A baixa histórica já foi revogada.");
        if (string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length > 500)
            throw new CatalogValidationException("Informe o motivo da revogação.");
        settlement.RevokedAt = DateTimeOffset.UtcNow;
        settlement.RevocationReason = input.Reason.Trim();
        Audit("liquidacao-historica.revogada", settlement.Id, JsonSerializer.Serialize(new
        {
            titleId = settlement.TitleId,
            amount = settlement.Amount,
            effectiveDate = settlement.EffectiveDate,
            reason = settlement.RevocationReason
        }));
        await db.SaveChangesAsync(cancellationToken);
        return Entry(settlement);
    }

    private async Task<PayableTitle> TitleAsync(Guid id, CancellationToken cancellationToken) =>
        await db.PayableTitles.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new CatalogNotFoundException();

    private async Task DemandAsync(bool write, CancellationToken cancellationToken)
    {
        if (!scope.IsAuthorized || actor.UserId is null || !await permissions.HasAsync(write ? "cadastros.editar" : "cadastros.visualizar", cancellationToken))
            throw new AccessDeniedException("Permissão financeira necessária no espaço ativo.");
    }

    private void Audit(string action, Guid entityId, string changesJson) => db.AuditRecords.Add(new AuditRecord
    {
        TenantId = scope.TenantId!.Value,
        SpaceId = scope.SpaceId!.Value,
        UserId = actor.UserId,
        CreatedBy = actor.UserId,
        Action = action,
        Entity = "HistoricalSettlement",
        EntityId = entityId,
        Instant = DateTimeOffset.UtcNow,
        CorrelationId = actor.CorrelationId,
        ChangesJson = changesJson
    });

    private static HistoricalSettlementEntry Entry(HistoricalSettlement x) =>
        new(x.Id, x.TitleId, x.PayerId, x.ApprovedBy, Money(x.Amount), x.EffectiveDate, x.Justification,
            x.PreviousReference, x.SourceStatus, x.RevokedAt, x.RevocationReason, x.Version);

    private static string Money(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);
    private static string? TrimToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
