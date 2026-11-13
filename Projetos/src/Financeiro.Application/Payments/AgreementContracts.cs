using System.ComponentModel.DataAnnotations;
using Financeiro.Application.Catalogs;

namespace Financeiro.Application.Payments;

public sealed record AgreementTitleInput(Guid TitleId, decimal NominalAmount, decimal Discount);
public sealed record AgreementSimulationInput(
    Guid ObligationId,
    [Required, MaxLength(200)] string Name,
    DateOnly SimulationDate,
    [MaxLength(1000)] string? Notes,
    IReadOnlyList<AgreementTitleInput> Titles);
public sealed record AgreementInput(
    Guid ObligationId,
    DateOnly? ProposalDate,
    DateOnly? ValidUntil,
    IReadOnlyList<AgreementTitleInput> Titles);
public sealed record AgreementConfirmationInput(
    Guid AccountId,
    Guid PayerId,
    DateOnly PaymentDate,
    [Required] string Currency,
    [Required, MaxLength(50)] string Method,
    [Required, MaxLength(200)] string IdempotencyKey,
    long? Version);
public sealed record AgreementTitleEntry(Guid TitleId, string NominalAmount, string Discount);
public sealed record AgreementSimulationEntry(
    Guid Id,
    Guid ObligationId,
    string Name,
    DateOnly SimulationDate,
    string ProposedAmount,
    string? Notes,
    IReadOnlyList<AgreementTitleEntry> Titles);
public sealed record AgreementEntry(
    Guid Id,
    Guid ObligationId,
    DateOnly? ProposalDate,
    DateOnly? ValidUntil,
    string ProposedAmount,
    string State,
    Guid? PaymentId,
    long Version,
    IReadOnlyList<AgreementTitleEntry> Titles);

public interface IAgreementService
{
    Task<PageResult<AgreementEntry>> ListAsync(PageRequest page, CancellationToken cancellationToken = default);
    Task<AgreementSimulationEntry> SimulateAsync(AgreementSimulationInput input, CancellationToken cancellationToken = default);
    Task<AgreementEntry> CreateAsync(AgreementInput input, CancellationToken cancellationToken = default);
    Task<AgreementEntry> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AgreementEntry> ConfirmAsync(Guid id, AgreementConfirmationInput input, CancellationToken cancellationToken = default);
}
