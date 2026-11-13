using System.ComponentModel.DataAnnotations;

namespace Financeiro.Application.Payments;

public sealed record HistoricalSettlementInput(
    decimal Amount,
    DateOnly? EffectiveDate,
    Guid? PayerId,
    [Required, MaxLength(1000)] string Justification,
    [MaxLength(200)] string? PreviousReference,
    [MaxLength(100)] string? SourceStatus);
public sealed record HistoricalSettlementRevocationInput([Required, MaxLength(500)] string Reason, long? Version);
public sealed record HistoricalSettlementEntry(
    Guid Id,
    Guid TitleId,
    Guid? PayerId,
    Guid ApprovedBy,
    string Amount,
    DateOnly? EffectiveDate,
    string Justification,
    string? PreviousReference,
    string? SourceStatus,
    DateTimeOffset? RevokedAt,
    string? RevocationReason,
    long Version);

public interface IHistoricalSettlementService
{
    Task<IReadOnlyList<HistoricalSettlementEntry>> ListAsync(Guid titleId, CancellationToken cancellationToken = default);
    Task<HistoricalSettlementEntry> CreateAsync(Guid titleId, HistoricalSettlementInput input, CancellationToken cancellationToken = default);
    Task<HistoricalSettlementEntry> RevokeAsync(Guid titleId, Guid settlementId, HistoricalSettlementRevocationInput input, CancellationToken cancellationToken = default);
}
