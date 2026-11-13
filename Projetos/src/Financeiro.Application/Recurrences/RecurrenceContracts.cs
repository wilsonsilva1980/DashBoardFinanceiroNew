using System.ComponentModel.DataAnnotations;
using Financeiro.Application.Catalogs;
using Financeiro.Domain.Entities;

namespace Financeiro.Application.Recurrences;

public sealed record RecurrenceInput(Guid ObligationId, DateOnly AnchorDate, int NominalDay, DateOnly? EndDate, bool Active = true, long? Version = null);

public sealed record RecurrenceEntry(Guid Id, Guid ObligationId, DateOnly AnchorDate, int NominalDay, DateOnly? EndDate, bool Active, long Version);

public sealed record RecurrencePriceInput(Guid RecurrenceId, DateOnly StartDate, DateOnly? EndDate, decimal CycleAmount, string Currency, int IntervalMonths, long? Version = null);

public sealed record RecurrencePriceEntry(Guid Id, Guid RecurrenceId, DateOnly StartDate, DateOnly? EndDate, decimal CycleAmount, string Currency, int IntervalMonths);

public sealed record RecurrenceGenerationInput(DateOnly From, DateOnly To);

public sealed record RecurrenceGenerationResult(int Created, int Skipped);

public interface IRecurrenceService
{
    Task<PageResult<RecurrenceEntry>> ListAsync(PageRequest page, CancellationToken cancellationToken = default);
    Task<RecurrenceEntry> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RecurrenceEntry> SaveAsync(Guid? id, RecurrenceInput input, CancellationToken cancellationToken = default);
    Task<RecurrencePriceEntry> SavePriceAsync(Guid recurrenceId, RecurrencePriceInput input, CancellationToken cancellationToken = default);
    Task<RecurrenceGenerationResult> GenerateHorizonAsync(Guid recurrenceId, RecurrenceGenerationInput input, CancellationToken cancellationToken = default);
}
