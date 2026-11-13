using System.ComponentModel.DataAnnotations;
using Financeiro.Application.Catalogs;
using Financeiro.Domain;
using Financeiro.Domain.Entities;

namespace Financeiro.Application.Obligations;

public sealed record ObligationInput(
    Guid? CategoryId,
    Guid? ResponsibleId,
    [Required, MaxLength(1000)] string Description,
    [MaxLength(200)] string? Creditor,
    [Required, MaxLength(50)] string Kind,
    DateOnly? PurchaseDate,
    [MaxLength(200)] string? PurchaseDateContext,
    int? DueDay,
    int? InstallmentCount,
    [Required] string Currency,
    bool Active = true,
    long? Version = null);

public sealed record ObligationEntry(
    Guid Id,
    Guid? CategoryId,
    Guid? ResponsibleId,
    string Description,
    string? Creditor,
    string Kind,
    DateOnly? PurchaseDate,
    string? PurchaseDateContext,
    int? DueDay,
    int? InstallmentCount,
    string Currency,
    bool Active,
    long Version);

public sealed record PayableTitleInput(
    Guid ObligationId,
    Guid? ResponsibleId,
    Guid? CardId,
    DateOnly Period,
    DateOnly? DueDate,
    int? InstallmentNumber,
    [Required, MaxLength(200)] string OccurrenceKey,
    decimal OriginalAmount,
    TitleState State = TitleState.Draft,
    long? Version = null);

public sealed record PayableTitleEntry(
    Guid Id,
    Guid ObligationId,
    Guid? ResponsibleId,
    Guid? CardId,
    DateOnly Period,
    DateOnly? DueDate,
    int? InstallmentNumber,
    string OccurrenceKey,
    string OriginalAmount,
    TitleState State,
    DateTimeOffset? CancelledAt,
    long Version,
    string Balance,
    string SettledAmount,
    string ExcessCredit,
    SettlementStatus SettlementStatus);

public sealed record PlanningEntryInput(Guid PersonId, decimal Amount);
public sealed record PlanningEntryEntry(Guid Id, Guid TitleId, Guid PersonId, string Amount, long Version);

public sealed record CardInvoiceInput(Guid CardId, Guid TitleId, DateOnly Period, string Mode, decimal? ReportedAmount, string? Notes, long? Version = null);
public sealed record CardInvoiceEntry(Guid Id, Guid CardId, Guid TitleId, DateOnly Period, string Mode, string? ReportedAmount, string? Notes, long Version);
public sealed record InvoicePurchaseInput(Guid InvoiceId, Guid? PurchaseObligationId, string Description, decimal Amount, int? InstallmentNumber, string OccurrenceKey, long? Version = null);
public sealed record InvoicePurchaseEntry(Guid Id, Guid InvoiceId, Guid? PurchaseObligationId, string Description, decimal Amount, int? InstallmentNumber, string OccurrenceKey, long Version);

public interface IObligationService
{
    Task<PageResult<ObligationEntry>> ListAsync(PageRequest page, CancellationToken cancellationToken = default);
    Task<ObligationEntry> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ObligationEntry> SaveAsync(Guid? id, ObligationInput input, CancellationToken cancellationToken = default);
    Task<PageResult<PayableTitleEntry>> ListTitlesAsync(PageRequest page, Guid? obligationId = null, bool includeForecast = false, DateOnly? asOf = null, Guid? cardId = null, CancellationToken cancellationToken = default);
    Task<PayableTitleEntry> GetTitleAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PayableTitleEntry> SaveTitleAsync(Guid? id, PayableTitleInput input, CancellationToken cancellationToken = default);
    Task<PlanningEntryEntry> SavePlanningEntryAsync(Guid titleId, PlanningEntryInput input, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlanningEntryEntry>> PlanningEntriesAsync(Guid titleId, CancellationToken cancellationToken = default);
    Task<PageResult<CardInvoiceEntry>> InvoicesAsync(PageRequest page, Guid? cardId, CancellationToken cancellationToken = default);
    Task<CardInvoiceEntry> GetInvoiceAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CardInvoiceEntry> SaveInvoiceAsync(Guid? id, CardInvoiceInput input, CancellationToken cancellationToken = default);
    Task<InvoicePurchaseEntry> SavePurchaseAsync(Guid? id, InvoicePurchaseInput input, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, long version, CancellationToken cancellationToken = default);
    Task DeleteTitleAsync(Guid id, long version, CancellationToken cancellationToken = default);
}
