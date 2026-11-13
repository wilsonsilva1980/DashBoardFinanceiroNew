using Financeiro.Application.Catalogs;

namespace Financeiro.Application.Accounts;

public sealed record AccountPositionInput(Guid AccountId, DateOnly? PositionDate, decimal? Balance, decimal? SavingsBalance, decimal? OverdraftUsed);
public sealed record AccountPositionEntry(Guid Id, Guid AccountId, DateOnly PositionDate, string Currency, string? Balance, string? SavingsBalance, string? OverdraftUsed, string ConfiguredOverdraftLimit, string? AvailableOverdraft, string? AccountNumber, string InstitutionName);
public interface IAccountPositionService
{
    Task<AccountPositionEntry> CreateAsync(AccountPositionInput input, CancellationToken cancellationToken = default);
    Task<PageResult<AccountPositionEntry>> ListAsync(PageRequest page, Guid? accountId = null, CancellationToken cancellationToken = default);
}
