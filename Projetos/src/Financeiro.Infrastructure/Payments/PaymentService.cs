using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Financeiro.Application.Access;
using Financeiro.Application.Catalogs;
using Financeiro.Application.Payments;
using Financeiro.Domain.Entities;
using Financeiro.Infrastructure.Access;
using Financeiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Infrastructure.Payments;

public sealed class PaymentService(FinanceiroDbContext db, FinancialScope scope, IRequestActor actor, PermissionEvaluator permissions, ITituloLockService locks) : IPaymentService
{
    private const string ConfirmOperation = "pagamento.confirmar";

    public async Task<PageResult<PaymentEntry>> ListAsync(PageRequest page, CancellationToken cancellationToken = default)
    {
        await DemandAsync(false, cancellationToken);
        var offset = page.Offset();
        var query = db.Payments.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip(offset).Take(page.PageSize).ToArrayAsync(cancellationToken);
        var allocations = await AllocationsAsync(rows.Select(x => x.Id).ToArray(), cancellationToken);
        return new(rows.Select(x => Entry(x, allocations.GetValueOrDefault(x.Id, []))).ToArray(), total, page.Page, page.PageSize);
    }

    public async Task<PaymentEntry> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await DemandAsync(false, cancellationToken);
        var payment = await PaymentAsync(id, cancellationToken);
        var allocations = await AllocationsAsync([id], cancellationToken);
        return Entry(payment, allocations.GetValueOrDefault(id, []));
    }

    public async Task<PaymentEntry> CreateDraftAsync(PaymentInput input, CancellationToken cancellationToken = default)
    {
        await DemandAsync(true, cancellationToken);
        Validate(input);
        var allocations = input.Allocations.GroupBy(x => x.TitleId).Select(group => group.Single()).ToArray();
        if (!await db.BankAccounts.AnyAsync(x => x.Id == input.AccountId, cancellationToken))
            throw new CatalogValidationException("Conta indisponível no espaço ativo.");
        if (!await db.Persons.AnyAsync(x => x.Id == input.PayerId, cancellationToken))
            throw new CatalogValidationException("Pagador indisponível no espaço ativo.");
        await ValidateTitlesAsync(allocations.Select(x => x.TitleId).ToArray(), input.Currency, cancellationToken);

        var payment = new Payment
        {
            TenantId = scope.TenantId!.Value,
            SpaceId = scope.SpaceId!.Value,
            CreatedBy = actor.UserId,
            AccountId = input.AccountId,
            PayerId = input.PayerId,
            PaymentDate = input.PaymentDate,
            Amount = input.Amount,
            Currency = input.Currency,
            Method = input.Method.Trim(),
            IdempotencyKey = input.IdempotencyKey.Trim(),
            State = PaymentState.Draft
        };
        db.Payments.Add(payment);
        foreach (var allocation in allocations)
            db.PaymentAllocations.Add(new PaymentAllocation
            {
                TenantId = scope.TenantId!.Value,
                SpaceId = scope.SpaceId!.Value,
                CreatedBy = actor.UserId,
                PaymentId = payment.Id,
                TitleId = allocation.TitleId,
                Amount = allocation.Amount
            });
        await db.SaveChangesAsync(cancellationToken);
        return Entry(payment, allocations.Select(x => new PaymentAllocationEntry(x.TitleId, Money(x.Amount))).ToArray());
    }

    public async Task<PaymentEntry> ConfirmAsync(Guid id, PaymentConfirmationInput input, CancellationToken cancellationToken = default)
    {
        await DemandAsync(true, cancellationToken);
        var payment = await PaymentAsync(id, cancellationToken);
        var allocations = await AllocationEntitiesAsync(id, cancellationToken);
        var payloadHash = PayloadHash(payment, allocations);
        var existing = await FindIdempotencyAsync(payment.IdempotencyKey, cancellationToken);
        if (existing is not null) return await ReplayAsync(existing, payloadHash, cancellationToken);
        CatalogValidation.Version(input.Version, payment.Version);
        if (payment.State != PaymentState.Draft)
            throw new RequestConflictException("O pagamento já foi confirmado ou revertido.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            db.IdempotencyRecords.Add(new IdempotencyRecord
            {
                TenantId = scope.TenantId!.Value,
                SpaceId = scope.SpaceId!.Value,
                CreatedBy = actor.UserId,
                ActorId = actor.UserId!.Value,
                Operation = ConfirmOperation,
                RequestKey = payment.IdempotencyKey,
                PayloadHash = payloadHash,
                ResultId = payment.Id
            });
            await db.SaveChangesAsync(cancellationToken);
            await locks.LockAsync(scope.TenantId!.Value, scope.SpaceId!.Value, allocations.Select(x => x.TitleId).ToArray(), cancellationToken);
            await db.Entry(payment).ReloadAsync(cancellationToken);
            await ValidateSettlementAsync(payment, allocations, cancellationToken);
            payment.State = PaymentState.Confirmed;
            Audit("pagamento.confirmado", payment.Id, JsonSerializer.Serialize(new
            {
                amount = payment.Amount,
                currency = payment.Currency,
                allocations = allocations.Select(x => new { titleId = x.TitleId, amount = x.Amount })
            }));
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Entry(payment, allocations.Select(x => new PaymentAllocationEntry(x.TitleId, Money(x.Amount))).ToArray());
        }
        catch (Exception exception) when (exception is DbUpdateException or DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            var record = await FindIdempotencyAsync(payment.IdempotencyKey, cancellationToken);
            if (record is not null && record.PayloadHash == payloadHash)
            {
                if (db.Entry(payment).State != EntityState.Detached) db.Entry(payment).State = EntityState.Detached;
                return await ReplayAsync(record, payloadHash, cancellationToken);
            }
            if (exception is DbUpdateConcurrencyException)
                throw new RequestConflictException("O pagamento foi alterado por outra operação. Recarregue antes de confirmar.");
            throw;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PaymentEntry> ReverseAsync(Guid id, PaymentReversalInput input, CancellationToken cancellationToken = default)
    {
        await DemandAsync(true, cancellationToken);
        var payment = await PaymentAsync(id, cancellationToken);
        CatalogValidation.Version(input.Version, payment.Version);
        if (payment.State != PaymentState.Confirmed)
            throw new RequestConflictException("Somente pagamentos confirmados podem ser revertidos.");
        if (string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length > 200)
            throw new CatalogValidationException("Informe o motivo da reversão.");
        payment.State = PaymentState.Reversed;
        payment.ReversedAt = DateTimeOffset.UtcNow;
        payment.ReversalReason = input.Reason.Trim();
        Audit("pagamento.revertido", payment.Id, JsonSerializer.Serialize(new
        {
            reason = payment.ReversalReason,
            amount = payment.Amount,
            currency = payment.Currency
        }));
        await db.SaveChangesAsync(cancellationToken);
        var allocations = await AllocationEntitiesAsync(id, cancellationToken);
        return Entry(payment, allocations.Select(x => new PaymentAllocationEntry(x.TitleId, Money(x.Amount))).ToArray());
    }

    private async Task<PaymentEntry> ReplayAsync(IdempotencyRecord record, string payloadHash, CancellationToken cancellationToken)
    {
        if (record.PayloadHash != payloadHash)
            throw new RequestConflictException("A chave de idempotência já foi usada com outro conteúdo.");
        return await GetAsync(record.ResultId, cancellationToken);
    }

    private async Task ValidateSettlementAsync(Payment payment, PaymentAllocation[] allocations, CancellationToken cancellationToken)
    {
        if (payment.State != PaymentState.Draft)
            throw new RequestConflictException("O pagamento já foi confirmado ou revertido.");
        if (allocations.Length == 0)
            throw new CatalogValidationException("Informe ao menos uma alocação para confirmar o pagamento.");
        if (allocations.Any(x => x.Amount <= 0))
            throw new CatalogValidationException("Cada alocação deve ter valor positivo.");
        if (allocations.Sum(x => x.Amount) != payment.Amount)
            throw new CatalogValidationException("A soma das alocações deve coincidir com o valor do pagamento.");
        await ValidateTitlesAsync(allocations.Select(x => x.TitleId).ToArray(), payment.Currency, cancellationToken);
    }

    private async Task ValidateTitlesAsync(Guid[] titleIds, string currency, CancellationToken cancellationToken)
    {
        var titles = await (from title in db.PayableTitles
                            join obligation in db.Obligations on title.ObligationId equals obligation.Id
                            where titleIds.Contains(title.Id)
                            select new { title.Id, title.State, obligation.Currency }).ToArrayAsync(cancellationToken);
        if (titles.Length != titleIds.Distinct().Count())
            throw new CatalogValidationException("Título indisponível no espaço ativo.");
        if (titles.Any(x => x.State == TitleState.Cancelled))
            throw new CatalogValidationException("Títulos cancelados não podem ser pagos.");
        if (titles.Any(x => x.Currency != currency))
            throw new CatalogValidationException("A moeda do pagamento deve coincidir com a moeda dos títulos.");
    }

    private async Task<IdempotencyRecord?> FindIdempotencyAsync(string requestKey, CancellationToken cancellationToken) =>
        await db.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(x =>
            x.ActorId == actor.UserId && x.Operation == ConfirmOperation && x.RequestKey == requestKey, cancellationToken);

    private async Task<PaymentAllocation[]> AllocationEntitiesAsync(Guid paymentId, CancellationToken cancellationToken) =>
        await db.PaymentAllocations.AsNoTracking().Where(x => x.PaymentId == paymentId).OrderBy(x => x.TitleId).ToArrayAsync(cancellationToken);

    private async Task<Dictionary<Guid, IReadOnlyList<PaymentAllocationEntry>>> AllocationsAsync(Guid[] paymentIds, CancellationToken cancellationToken)
    {
        if (paymentIds.Length == 0) return [];
        var rows = await db.PaymentAllocations.AsNoTracking()
            .Where(x => paymentIds.Contains(x.PaymentId))
            .OrderBy(x => x.PaymentId).ThenBy(x => x.TitleId)
            .ToArrayAsync(cancellationToken);
        return rows.GroupBy(x => x.PaymentId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<PaymentAllocationEntry>)group.Select(x => new PaymentAllocationEntry(x.TitleId, Money(x.Amount))).ToArray());
    }

    private async Task<Payment> PaymentAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Payments.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new CatalogNotFoundException();

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
        Entity = "Payment",
        EntityId = entityId,
        Instant = DateTimeOffset.UtcNow,
        CorrelationId = actor.CorrelationId,
        ChangesJson = changesJson
    });

    private static string PayloadHash(Payment payment, PaymentAllocation[] allocations)
    {
        var canonical = new StringBuilder()
            .Append(payment.Id).Append('|')
            .Append(Money(payment.Amount)).Append('|')
            .Append(payment.Currency).Append('|');
        foreach (var allocation in allocations.OrderBy(x => x.TitleId))
            canonical.Append(allocation.TitleId).Append(':').Append(Money(allocation.Amount)).Append(';');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
    }

    private static PaymentEntry Entry(Payment x, IReadOnlyList<PaymentAllocationEntry> allocations) =>
        new(x.Id, x.AccountId, x.PayerId, x.PaymentDate, Money(x.Amount), x.Currency, x.Method, x.State, x.IdempotencyKey, x.ReversedAt, x.ReversalReason, x.Version, allocations);

    private static string Money(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);

    private static void Validate(PaymentInput input)
    {
        CatalogValidation.Money(input.Amount);
        if (input.Amount <= 0) throw new CatalogValidationException("Informe um valor de pagamento positivo.");
        CatalogValidation.Currency(input.Currency);
        if (string.IsNullOrWhiteSpace(input.Method) || input.Method.Trim().Length > 50)
            throw new CatalogValidationException("Informe a forma de pagamento.");
        if (string.IsNullOrWhiteSpace(input.IdempotencyKey) || input.IdempotencyKey.Trim().Length > 200)
            throw new CatalogValidationException("Informe uma chave de idempotência válida.");
        if (input.Allocations.GroupBy(x => x.TitleId).Any(group => group.Count() > 1))
            throw new CatalogValidationException("Cada título pode receber apenas uma alocação por pagamento.");
        foreach (var allocation in input.Allocations)
        {
            CatalogValidation.Money(allocation.Amount);
            if (allocation.Amount <= 0) throw new CatalogValidationException("Cada alocação deve ter valor positivo.");
        }
    }
}
