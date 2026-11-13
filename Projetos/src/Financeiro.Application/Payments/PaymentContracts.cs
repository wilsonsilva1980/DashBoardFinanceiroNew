using System.ComponentModel.DataAnnotations;
using Financeiro.Application.Catalogs;
using Financeiro.Domain.Entities;

namespace Financeiro.Application.Payments;

public sealed record PaymentAllocationInput(Guid TitleId, decimal Amount);
public sealed record PaymentInput(
    Guid AccountId,
    Guid PayerId,
    DateOnly PaymentDate,
    decimal Amount,
    [Required] string Currency,
    [Required, MaxLength(50)] string Method,
    [Required, MaxLength(200)] string IdempotencyKey,
    IReadOnlyList<PaymentAllocationInput> Allocations);
public sealed record PaymentConfirmationInput(long? Version);
public sealed record PaymentReversalInput([Required, MaxLength(200)] string Reason, long? Version);
public sealed record PaymentAllocationEntry(Guid TitleId, string Amount);
public sealed record PaymentEntry(
    Guid Id,
    Guid AccountId,
    Guid PayerId,
    DateOnly PaymentDate,
    string Amount,
    string Currency,
    string Method,
    PaymentState State,
    string IdempotencyKey,
    DateTimeOffset? ReversedAt,
    string? ReversalReason,
    long Version,
    IReadOnlyList<PaymentAllocationEntry> Allocations);

public interface IPaymentService
{
    Task<PageResult<PaymentEntry>> ListAsync(PageRequest page, CancellationToken cancellationToken = default);
    Task<PaymentEntry> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentEntry> CreateDraftAsync(PaymentInput input, CancellationToken cancellationToken = default);
    Task<PaymentEntry> ConfirmAsync(Guid id, PaymentConfirmationInput input, CancellationToken cancellationToken = default);
    Task<PaymentEntry> ReverseAsync(Guid id, PaymentReversalInput input, CancellationToken cancellationToken = default);
}
