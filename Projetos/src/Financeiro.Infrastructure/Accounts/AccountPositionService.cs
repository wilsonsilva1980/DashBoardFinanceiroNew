using System.Globalization;
using Financeiro.Application.Access;
using Financeiro.Application.Accounts;
using Financeiro.Application.Catalogs;
using Financeiro.Domain.Entities;
using Financeiro.Infrastructure.Access;
using Financeiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Infrastructure.Accounts;

public sealed class AccountPositionService(FinanceiroDbContext db, FinancialScope scope, IRequestActor actor, PermissionEvaluator permissions) : IAccountPositionService
{
    public async Task<AccountPositionEntry> CreateAsync(AccountPositionInput input, CancellationToken cancellationToken = default)
    {
        if (!scope.IsAuthorized || actor.UserId is null || !await permissions.HasAsync("cadastros.editar", cancellationToken)) throw new AccessDeniedException("Permissão de edição necessária.");
        if (input.PositionDate is null) throw new CatalogValidationException("Informe a data da posição.");
        SignedMoney(input.Balance);
        SignedMoney(input.SavingsBalance);
        CatalogValidation.Money(input.OverdraftUsed);
        var account = await db.BankAccounts.SingleOrDefaultAsync(x => x.Id == input.AccountId, cancellationToken) ?? throw new CatalogValidationException("Conta indisponível no espaço ativo.");
        var position = new AccountPosition
        {
            TenantId = scope.TenantId!.Value, SpaceId = scope.SpaceId!.Value, CreatedBy = actor.UserId,
            AccountId = account.Id, PositionDate = input.PositionDate.Value, Balance = input.Balance,
            SavingsBalance = input.SavingsBalance, OverdraftUsed = input.OverdraftUsed
        };
        db.AccountPositions.Add(position);
        await db.SaveChangesAsync(cancellationToken);
        var institution = await db.Institutions.Where(x => x.Id == account.InstitutionId).Select(x => x.Name).SingleAsync(cancellationToken);
        return Entry(position, account.Currency, account.OverdraftLimit, account.Number, institution);
    }

    public async Task<PageResult<AccountPositionEntry>> ListAsync(PageRequest page, Guid? accountId = null, CancellationToken cancellationToken = default)
    {
        if (!scope.IsAuthorized || !await permissions.HasAsync("cadastros.visualizar", cancellationToken)) throw new AccessDeniedException("Permissão de leitura necessária.");
        var offset = page.Offset();
        var query = from position in db.AccountPositions.AsNoTracking()
                    join account in db.BankAccounts.AsNoTracking() on position.AccountId equals account.Id
                    join institution in db.Institutions.AsNoTracking() on account.InstitutionId equals institution.Id
                    where accountId == null || position.AccountId == accountId
                    select new { Position = position, account.Currency, account.OverdraftLimit, account.Number, InstitutionName = institution.Name };
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.Position.PositionDate).ThenByDescending(x => x.Position.CreatedAt).ThenBy(x => x.Position.Id)
            .Skip(offset).Take(page.PageSize).ToArrayAsync(cancellationToken);
        return new(rows.Select(x => Entry(x.Position, x.Currency, x.OverdraftLimit, x.Number, x.InstitutionName)).ToArray(), total, page.Page, page.PageSize);
    }

    private static void SignedMoney(decimal? value)
    {
        if (value.HasValue && (value < -99999999999999999.99m || value > 99999999999999999.99m || decimal.Round(value.Value, 2) != value))
            throw new CatalogValidationException("Informe valores com até duas casas decimais dentro da precisão monetária.");
    }
    private static string? Money(decimal? value) => value?.ToString("0.00", CultureInfo.InvariantCulture);
    private static AccountPositionEntry Entry(AccountPosition position, string currency, decimal limit, string? accountNumber, string institutionName) => new(position.Id, position.AccountId, position.PositionDate, currency,
        Money(position.Balance), Money(position.SavingsBalance), Money(position.OverdraftUsed), Money(limit)!,
        position.OverdraftUsed.HasValue ? Money(limit - position.OverdraftUsed.Value) : null, accountNumber, institutionName);
}
