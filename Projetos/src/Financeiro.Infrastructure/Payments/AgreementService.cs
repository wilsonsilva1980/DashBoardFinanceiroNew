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

/// <summary>
/// Settlement agreements reference existing payable titles; confirming one applies its
/// discounts and payment allocations atomically without creating a second payable amount.
/// Simulations stay nonfinancial.
/// </summary>
public sealed class AgreementService(FinanceiroDbContext db, FinancialScope scope, IRequestActor actor, PermissionEvaluator permissions, IAuditWriter? auditWriter = null) : IAgreementService
{
    public async Task<PageResult<AgreementEntry>> ListAsync(PageRequest page, CancellationToken cancellationToken = default)
    {
        await DemandAsync(false, cancellationToken);
        var offset = page.Offset();
        var query = db.SettlementAgreements.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip(offset).Take(page.PageSize).ToArrayAsync(cancellationToken);
        var ids = rows.Select(x => x.Id).ToArray();
        var lines = ids.Length == 0
            ? []
            : await db.AgreementTitles.AsNoTracking().Where(x => ids.Contains(x.AgreementId)).OrderBy(x => x.TitleId).ToArrayAsync(cancellationToken);
        var grouped = lines.GroupBy(x => x.AgreementId).ToDictionary(group => group.Key, group => (IReadOnlyList<AgreementTitle>)group.ToArray());
        return new(rows.Select(x => Entry(x, grouped.GetValueOrDefault(x.Id, []))).ToArray(), total, page.Page, page.PageSize);
    }
    public async Task<AgreementSimulationEntry> SimulateAsync(AgreementSimulationInput input, CancellationToken cancellationToken = default)
    {
        await DemandAsync(true, cancellationToken);
        var obligation = await ObligationAsync(input.ObligationId, cancellationToken);
        CatalogValidation.Name(input.Name);
        CatalogValidation.Text(input.Notes, 1000);
        var lines = await ValidateLinesAsync(input.Titles, obligation, cancellationToken);

        var scenario = new SettlementScenario
        {
            TenantId = scope.TenantId!.Value,
            SpaceId = scope.SpaceId!.Value,
            CreatedBy = actor.UserId,
            ObligationId = obligation.Id,
            Name = input.Name.Trim(),
            SimulationDate = input.SimulationDate,
            ProposedAmount = lines.Sum(x => x.Net),
            Notes = TrimToNull(input.Notes)
        };
        db.SettlementScenarios.Add(scenario);
        foreach (var line in lines)
            db.ScenarioTitles.Add(new ScenarioTitle
            {
                TenantId = scope.TenantId!.Value,
                SpaceId = scope.SpaceId!.Value,
                CreatedBy = actor.UserId,
                ScenarioId = scenario.Id,
                TitleId = line.TitleId,
                NominalAmount = line.NominalAmount,
                Discount = line.Discount
            });
        // A simulation never touches balances, payments, or period cash-flow results.
        await db.SaveChangesAsync(cancellationToken);
        return new(scenario.Id, scenario.ObligationId, scenario.Name, scenario.SimulationDate, Money(scenario.ProposedAmount), scenario.Notes,
            lines.Select(x => new AgreementTitleEntry(x.TitleId, Money(x.NominalAmount), Money(x.Discount))).ToArray());
    }

    public async Task<AgreementEntry> CreateAsync(AgreementInput input, CancellationToken cancellationToken = default)
    {
        await DemandAsync(true, cancellationToken);
        var obligation = await ObligationAsync(input.ObligationId, cancellationToken);
        if (input.ValidUntil is { } validUntil && input.ProposalDate is { } proposalDate && validUntil < proposalDate)
            throw new CatalogValidationException("A validade do acordo não pode anteceder a proposta.");
        var lines = await ValidateLinesAsync(input.Titles, obligation, cancellationToken);

        var agreement = new SettlementAgreement
        {
            TenantId = scope.TenantId!.Value,
            SpaceId = scope.SpaceId!.Value,
            CreatedBy = actor.UserId,
            ObligationId = obligation.Id,
            ProposalDate = input.ProposalDate,
            ValidUntil = input.ValidUntil,
            ProposedAmount = lines.Sum(x => x.Net),
            State = "Draft"
        };
        db.SettlementAgreements.Add(agreement);
        AddLines(agreement.Id, lines);
        await db.SaveChangesAsync(cancellationToken);
        return Entry(agreement, lines);
    }

    public async Task<AgreementEntry> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await DemandAsync(false, cancellationToken);
        var agreement = await AgreementAsync(id, cancellationToken);
        return Entry(agreement, await LinesAsync(agreement.Id, cancellationToken));
    }

    public async Task<AgreementEntry> ConfirmAsync(Guid id, AgreementConfirmationInput input, CancellationToken cancellationToken = default)
    {
        await DemandAsync(true, cancellationToken);
        var agreement = await AgreementAsync(id, cancellationToken);
        if (agreement.State == "Confirmed") return Entry(agreement, await LinesAsync(agreement.Id, cancellationToken));
        CatalogValidation.Version(input.Version, agreement.Version);
        if (agreement.State != "Draft")
            throw new RequestConflictException("O acordo não está mais em rascunho.");
        var obligation = await ObligationAsync(agreement.ObligationId, cancellationToken);
        CatalogValidation.Currency(input.Currency);
        if (input.Currency != obligation.Currency)
            throw new CatalogValidationException("A moeda do acordo deve coincidir com a moeda da obrigação.");
        if (string.IsNullOrWhiteSpace(input.Method) || input.Method.Trim().Length > 50)
            throw new CatalogValidationException("Informe a forma de pagamento.");
        if (string.IsNullOrWhiteSpace(input.IdempotencyKey) || input.IdempotencyKey.Trim().Length > 200)
            throw new CatalogValidationException("Informe uma chave de idempotência válida.");
        if (!await db.BankAccounts.AnyAsync(x => x.Id == input.AccountId, cancellationToken))
            throw new CatalogValidationException("Conta indisponível no espaço ativo.");
        if (!await db.Persons.AnyAsync(x => x.Id == input.PayerId, cancellationToken))
            throw new CatalogValidationException("Pagador indisponível no espaço ativo.");

        var lines = await TitleLinesAsync(agreement.Id, cancellationToken);
        if (lines.Length == 0) throw new CatalogValidationException("Informe ao menos um título no acordo.");
        var titleIds = lines.Select(x => x.TitleId).ToArray();
        var titles = await db.PayableTitles.Where(x => titleIds.Contains(x.Id)).ToArrayAsync(cancellationToken);
        if (titles.Length != titleIds.Length)
            throw new CatalogValidationException("Título indisponível no espaço ativo.");
        if (titles.Any(x => x.State == TitleState.Cancelled))
            throw new CatalogValidationException("Títulos cancelados não podem ser acordados.");
        var net = lines.Sum(x => x.Net);
        if (net <= 0) throw new CatalogValidationException("O valor líquido do acordo deve ser positivo.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var payment = new Payment
            {
                TenantId = scope.TenantId!.Value,
                SpaceId = scope.SpaceId!.Value,
                CreatedBy = actor.UserId,
                AccountId = input.AccountId,
                PayerId = input.PayerId,
                PaymentDate = input.PaymentDate,
                Amount = net,
                Currency = input.Currency,
                Method = input.Method.Trim(),
                IdempotencyKey = input.IdempotencyKey.Trim(),
                State = PaymentState.Confirmed
            };
            db.Payments.Add(payment);
            foreach (var line in lines)
            {
                db.PaymentAllocations.Add(new PaymentAllocation
                {
                    TenantId = scope.TenantId!.Value,
                    SpaceId = scope.SpaceId!.Value,
                    CreatedBy = actor.UserId,
                    PaymentId = payment.Id,
                    TitleId = line.TitleId,
                    Amount = line.NominalAmount - line.Discount
                });
            }
            agreement.State = "Confirmed";
            agreement.PaymentId = payment.Id;
            Writer.Record("acordo.confirmado", "SettlementAgreement", agreement.Id, new
            {
                paymentId = payment.Id,
                amount = net,
                currency = input.Currency,
                discounts = lines.Select(x => new { titleId = x.TitleId, discount = x.Discount })
            });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            var current = await AgreementAsync(agreement.Id, cancellationToken);
            if (current.State == "Confirmed") return Entry(current, await LinesAsync(current.Id, cancellationToken));
            throw new RequestConflictException("O acordo foi alterado. Recarregue antes de confirmar.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return Entry(agreement, lines);
    }

    private void AddLines(Guid agreementId, Line[] lines)
    {
        foreach (var line in lines)
            db.AgreementTitles.Add(new AgreementTitle
            {
                TenantId = scope.TenantId!.Value,
                SpaceId = scope.SpaceId!.Value,
                CreatedBy = actor.UserId,
                AgreementId = agreementId,
                TitleId = line.TitleId,
                NominalAmount = line.NominalAmount,
                Discount = line.Discount
            });
    }

    private sealed record Line(Guid TitleId, decimal NominalAmount, decimal Discount)
    {
        public decimal Net => NominalAmount - Discount;
    }

    private async Task<Line[]> ValidateLinesAsync(IReadOnlyList<AgreementTitleInput> inputs, Obligation obligation, CancellationToken cancellationToken)
    {
        if (inputs.Count == 0) throw new CatalogValidationException("Informe ao menos um título no acordo.");
        if (inputs.GroupBy(x => x.TitleId).Any(group => group.Count() > 1))
            throw new CatalogValidationException("Cada título pode aparecer apenas uma vez no acordo.");
        var titleIds = inputs.Select(x => x.TitleId).ToArray();
        var titles = await db.PayableTitles.Where(x => titleIds.Contains(x.Id) && x.ObligationId == obligation.Id).ToArrayAsync(cancellationToken);
        if (titles.Length != titleIds.Length)
            throw new CatalogValidationException("Título indisponível na obrigação informada.");
        if (titles.Any(x => x.State == TitleState.Cancelled))
            throw new CatalogValidationException("Títulos cancelados não podem ser acordados.");
        var lines = new Line[inputs.Count];
        for (var index = 0; index < inputs.Count; index++)
        {
            var input = inputs[index];
            var title = titles.Single(x => x.Id == input.TitleId);
            CatalogValidation.Money(input.NominalAmount);
            CatalogValidation.Money(input.Discount);
            if (input.NominalAmount <= 0) throw new CatalogValidationException("Informe o valor nominal do título.");
            if (input.NominalAmount != title.OriginalAmount)
                throw new CatalogValidationException("O valor nominal deve coincidir com o valor original do título.");
            if (input.Discount > input.NominalAmount)
                throw new CatalogValidationException("O desconto não pode exceder o valor nominal.");
            lines[index] = new Line(input.TitleId, input.NominalAmount, input.Discount);
        }
        return lines;
    }

    private async Task<Line[]> TitleLinesAsync(Guid agreementId, CancellationToken cancellationToken) =>
        (await LinesAsync(agreementId, cancellationToken))
            .Select(x => new Line(x.TitleId, x.NominalAmount, x.Discount))
            .ToArray();

    private async Task<AgreementTitle[]> LinesAsync(Guid agreementId, CancellationToken cancellationToken) =>
        await db.AgreementTitles.AsNoTracking().Where(x => x.AgreementId == agreementId).OrderBy(x => x.TitleId).ToArrayAsync(cancellationToken);

    private async Task<SettlementAgreement> AgreementAsync(Guid id, CancellationToken cancellationToken) =>
        await db.SettlementAgreements.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new CatalogNotFoundException();

    private async Task<Obligation> ObligationAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Obligations.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new CatalogNotFoundException();

    private async Task DemandAsync(bool write, CancellationToken cancellationToken)
    {
        if (!scope.IsAuthorized || actor.UserId is null || !await permissions.HasAsync(write ? "cadastros.editar" : "cadastros.visualizar", cancellationToken))
            throw new AccessDeniedException("Permissão financeira necessária no espaço ativo.");
    }

    private IAuditWriter Writer => auditWriter ?? new AuditWriter(db, scope, actor);

    private static AgreementEntry Entry(SettlementAgreement x, IReadOnlyList<AgreementTitle> titles) =>
        new(x.Id, x.ObligationId, x.ProposalDate, x.ValidUntil, Money(x.ProposedAmount), x.State, x.PaymentId, x.Version,
            titles.Select(line => new AgreementTitleEntry(line.TitleId, Money(line.NominalAmount), Money(line.Discount))).ToArray());

    private static AgreementEntry Entry(SettlementAgreement x, IReadOnlyList<Line> titles) =>
        new(x.Id, x.ObligationId, x.ProposalDate, x.ValidUntil, Money(x.ProposedAmount), x.State, x.PaymentId, x.Version,
            titles.Select(line => new AgreementTitleEntry(line.TitleId, Money(line.NominalAmount), Money(line.Discount))).ToArray());

    private static string Money(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);
    private static string? TrimToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
