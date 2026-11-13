using System.Globalization;
using Financeiro.Application.Access;
using Financeiro.Application.Catalogs;
using Financeiro.Domain.Entities;
using Financeiro.Infrastructure.Access;
using Financeiro.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Financeiro.Infrastructure.Catalogs;

public sealed class CatalogService(FinanceiroDbContext db, FinancialScope scope, IRequestActor actor, PermissionEvaluator permissions) : ICatalogService
{
    public Task<PageResult<NamedCatalogEntry>> ListAsync(NamedCatalogKind kind, PageRequest page, CancellationToken cancellationToken = default) => kind switch
    {
        NamedCatalogKind.People => ListNamedAsync<Person>(page, cancellationToken),
        NamedCatalogKind.Categories => ListNamedAsync<Category>(page, cancellationToken),
        NamedCatalogKind.Institutions => ListNamedAsync<Institution>(page, cancellationToken),
        NamedCatalogKind.CardBrands => ListNamedAsync<CardBrand>(page, cancellationToken),
        _ => throw new CatalogValidationException("Cadastro inválido.")
    };
    public Task<NamedCatalogEntry> GetAsync(NamedCatalogKind kind, Guid id, CancellationToken cancellationToken = default) => kind switch
    {
        NamedCatalogKind.People => GetNamedAsync<Person>(id, cancellationToken),
        NamedCatalogKind.Categories => GetNamedAsync<Category>(id, cancellationToken),
        NamedCatalogKind.Institutions => GetNamedAsync<Institution>(id, cancellationToken),
        NamedCatalogKind.CardBrands => GetNamedAsync<CardBrand>(id, cancellationToken),
        _ => throw new CatalogValidationException("Cadastro inválido.")
    };
    public Task<NamedCatalogEntry> CreateAsync(NamedCatalogKind kind, NamedCatalogInput input, CancellationToken cancellationToken = default) => SaveNamedAsync(kind, null, input, cancellationToken);
    public Task<NamedCatalogEntry> UpdateAsync(NamedCatalogKind kind, Guid id, NamedCatalogInput input, CancellationToken cancellationToken = default) => SaveNamedAsync(kind, id, input, cancellationToken);
    private Task<NamedCatalogEntry> SaveNamedAsync(NamedCatalogKind kind, Guid? id, NamedCatalogInput input, CancellationToken cancellationToken) => kind switch
    {
        NamedCatalogKind.People => SaveNamedAsync<Person>(id, input, cancellationToken),
        NamedCatalogKind.Categories => SaveNamedAsync<Category>(id, input, cancellationToken),
        NamedCatalogKind.Institutions => SaveNamedAsync<Institution>(id, input, cancellationToken),
        NamedCatalogKind.CardBrands => SaveNamedAsync<CardBrand>(id, input, cancellationToken),
        _ => throw new CatalogValidationException("Cadastro inválido.")
    };
    public Task DeleteAsync(NamedCatalogKind kind, Guid id, long version, CancellationToken cancellationToken = default) => kind switch
    {
        NamedCatalogKind.People => DeleteAsync<Person>(id, version, cancellationToken),
        NamedCatalogKind.Categories => DeleteAsync<Category>(id, version, cancellationToken),
        NamedCatalogKind.Institutions => DeleteAsync<Institution>(id, version, cancellationToken),
        NamedCatalogKind.CardBrands => DeleteAsync<CardBrand>(id, version, cancellationToken),
        _ => throw new CatalogValidationException("Cadastro inválido.")
    };

    private async Task DemandAsync(bool write, CancellationToken cancellationToken)
    {
        if (!scope.IsAuthorized || actor.UserId is null || !await permissions.HasAsync(write ? "cadastros.editar" : "cadastros.visualizar", cancellationToken))
            throw new AccessDeniedException("Permissão de cadastro necessária no espaço ativo.");
    }
    private T New<T>() where T : ScopedEntity, new() => new() { TenantId = scope.TenantId!.Value, SpaceId = scope.SpaceId!.Value, CreatedBy = actor.UserId };
    private async Task<T> FindAsync<T>(Guid id, CancellationToken cancellationToken) where T : ScopedEntity =>
        await db.Set<T>().SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new CatalogNotFoundException();
    private static NamedCatalogEntry Named<T>(T row) where T : ScopedEntity, INamedCatalog =>
        new(row.Id, row.Name, row.Active, row.Version, (row as Category)?.Nature, (row as Institution)?.Code);

    private async Task<PageResult<NamedCatalogEntry>> ListNamedAsync<T>(PageRequest page, CancellationToken cancellationToken) where T : ScopedEntity, INamedCatalog
    {
        await DemandAsync(false, cancellationToken);
        var offset = page.Offset();
        var query = db.Set<T>().AsNoTracking();
        if (!page.IncludeInactive) query = query.Where(x => x.Active);
        if (!string.IsNullOrWhiteSpace(page.Search))
        {
            var search = page.Search.Trim().ToUpperInvariant();
            query = query.Where(x => x.Name.ToUpper().Contains(search));
        }
        var count = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip(offset).Take(page.PageSize).ToArrayAsync(cancellationToken);
        return new(rows.Select(Named).ToArray(), count, page.Page, page.PageSize);
    }
    private async Task<NamedCatalogEntry> GetNamedAsync<T>(Guid id, CancellationToken cancellationToken) where T : ScopedEntity, INamedCatalog
    {
        await DemandAsync(false, cancellationToken);
        return Named(await FindAsync<T>(id, cancellationToken));
    }
    private async Task<NamedCatalogEntry> SaveNamedAsync<T>(Guid? id, NamedCatalogInput input, CancellationToken cancellationToken) where T : ScopedEntity, INamedCatalog, new()
    {
        await DemandAsync(true, cancellationToken);
        var name = CatalogValidation.Name(input.Name);
        var entity = id.HasValue ? await FindAsync<T>(id.Value, cancellationToken) : New<T>();
        if (id.HasValue) CatalogValidation.Version(input.Version, entity.Version);
        if (entity is CardBrand && await db.CardBrands.AnyAsync(x => x.Name == name && x.Id != entity.Id, cancellationToken)) throw new CatalogNameConflictException();
        entity.Name = name;
        entity.Active = input.Active;
        if (entity is Category category)
        {
            var nature = input.Nature ?? category.Nature;
            if (nature is not ("Fixed" or "Variable" or "Unclassified")) throw new CatalogValidationException("Natureza da categoria inválida.");
            category.Nature = nature;
        }
        if (entity is Institution institution)
        {
            CatalogValidation.Text(input.Code, 20);
            institution.Code = input.Code;
        }
        if (!id.HasValue) db.Add(entity);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (entity is CardBrand && IsUniqueConflict(exception)) { throw new CatalogNameConflictException(); }
        return Named(entity);
    }

    public async Task<PageResult<BankAccountEntry>> AccountsAsync(PageRequest page, CancellationToken cancellationToken = default)
    {
        await DemandAsync(false, cancellationToken);
        var offset = page.Offset();
        var query = db.BankAccounts.AsNoTracking();
        if (!page.IncludeInactive) query = query.Where(x => x.Active);
        if (!string.IsNullOrWhiteSpace(page.Search))
        {
            var search = page.Search.Trim();
            query = query.Where(x => (x.Number != null && x.Number.Contains(search)) || (x.Agency != null && x.Agency.Contains(search)));
        }
        var count = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(x => x.Id).Skip(offset).Take(page.PageSize).ToArrayAsync(cancellationToken);
        return new(rows.Select(Account).ToArray(), count, page.Page, page.PageSize);
    }
    public async Task<BankAccountEntry> AccountAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await DemandAsync(false, cancellationToken);
        return Account(await FindAsync<BankAccount>(id, cancellationToken));
    }
    public async Task<BankAccountEntry> SaveAccountAsync(Guid? id, BankAccountInput input, CancellationToken cancellationToken = default)
    {
        await DemandAsync(true, cancellationToken);
        CatalogValidation.Currency(input.Currency);
        CatalogValidation.Money(input.OverdraftLimit);
        CatalogValidation.Text(input.Type, 30);
        CatalogValidation.Text(input.Agency, 30);
        CatalogValidation.Text(input.Number, 60);
        CatalogValidation.Text(input.CheckDigit, 10);
        var entity = id.HasValue ? await FindAsync<BankAccount>(id.Value, cancellationToken) : New<BankAccount>();
        if (id.HasValue) CatalogValidation.Version(input.Version, entity.Version);
        if (!await db.Institutions.AnyAsync(x => x.Id == input.InstitutionId, cancellationToken) ||
            (input.HolderId.HasValue && !await db.Persons.AnyAsync(x => x.Id == input.HolderId, cancellationToken)))
            throw new CatalogValidationException("Instituição ou titular indisponível no espaço ativo.");
        entity.InstitutionId = input.InstitutionId;
        entity.HolderId = input.HolderId;
        entity.Type = input.Type;
        // Identifiers are literal text; do not trim or convert them to numbers.
        entity.Agency = input.Agency;
        entity.Number = input.Number;
        entity.CheckDigit = input.CheckDigit;
        if (id.HasValue && entity.Currency != input.Currency &&
            (await db.AccountPositions.AnyAsync(x => x.AccountId == entity.Id, cancellationToken) || await db.Payments.AnyAsync(x => x.AccountId == entity.Id, cancellationToken)))
            throw new CatalogValidationException("A moeda de uma conta com histórico não pode ser alterada. Cadastre outra conta para a nova moeda.");
        entity.Currency = input.Currency;
        entity.OverdraftLimit = input.OverdraftLimit;
        entity.Active = input.Active;
        if (!id.HasValue) db.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Account(entity);
    }
    public Task DeleteAccountAsync(Guid id, long version, CancellationToken cancellationToken = default) => DeleteAsync<BankAccount>(id, version, cancellationToken);

    public async Task<PageResult<CreditCardEntry>> CardsAsync(PageRequest page, CancellationToken cancellationToken = default)
    {
        await DemandAsync(false, cancellationToken);
        var offset = page.Offset();
        var query = db.CreditCards.AsNoTracking();
        if (!page.IncludeInactive) query = query.Where(x => x.Active);
        if (!string.IsNullOrWhiteSpace(page.Search))
        {
            var search = page.Search.Trim().ToUpperInvariant();
            query = query.Where(x => x.Name.ToUpper().Contains(search));
        }
        var count = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip(offset).Take(page.PageSize).ToArrayAsync(cancellationToken);
        return new(rows.Select(Card).ToArray(), count, page.Page, page.PageSize);
    }
    public async Task<CreditCardEntry> CardAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await DemandAsync(false, cancellationToken);
        return Card(await FindAsync<CreditCard>(id, cancellationToken));
    }
    public async Task<CreditCardEntry> SaveCardAsync(Guid? id, CreditCardInput input, CancellationToken cancellationToken = default)
    {
        await DemandAsync(true, cancellationToken);
        var name = CatalogValidation.Name(input.Name, 120);
        CatalogValidation.Day(input.DueDay);
        CatalogValidation.Day(input.ClosingDay);
        foreach (var value in new[] { input.CreditLimit, input.CashLimit, input.ForeignCashLimit, input.PixLimit, input.BillLimit }) CatalogValidation.Money(value);
        var entity = id.HasValue ? await FindAsync<CreditCard>(id.Value, cancellationToken) : New<CreditCard>();
        if (id.HasValue) CatalogValidation.Version(input.Version, entity.Version);
        if (!await db.Institutions.AnyAsync(x => x.Id == input.InstitutionId, cancellationToken) ||
            (input.HolderId.HasValue && !await db.Persons.AnyAsync(x => x.Id == input.HolderId, cancellationToken)) ||
            (input.BrandId.HasValue && !await db.CardBrands.AnyAsync(x => x.Id == input.BrandId, cancellationToken)))
            throw new CatalogValidationException("Instituição, titular ou bandeira indisponível no espaço ativo.");
        entity.InstitutionId = input.InstitutionId;
        entity.BrandId = input.BrandId;
        entity.HolderId = input.HolderId;
        entity.Name = name;
        entity.DueDay = input.DueDay;
        entity.ClosingDay = input.ClosingDay;
        entity.CreditLimit = input.CreditLimit;
        entity.CashLimit = input.CashLimit;
        entity.ForeignCashLimit = input.ForeignCashLimit;
        entity.PixLimit = input.PixLimit;
        entity.BillLimit = input.BillLimit;
        entity.Active = input.Active;
        if (!id.HasValue) db.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Card(entity);
    }
    public Task DeleteCardAsync(Guid id, long version, CancellationToken cancellationToken = default) => DeleteAsync<CreditCard>(id, version, cancellationToken);

    private async Task DeleteAsync<T>(Guid id, long version, CancellationToken cancellationToken) where T : ScopedEntity
    {
        await DemandAsync(true, cancellationToken);
        var entity = await FindAsync<T>(id, cancellationToken);
        CatalogValidation.Version(version, entity.Version);
        db.Remove(entity);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (IsForeignKeyConflict(exception)) { throw new ReferencedCatalogException(); }
    }
    private static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    private static string? Money(decimal? value) => value.HasValue ? Money(value.Value) : null;
    private static BankAccountEntry Account(BankAccount x) => new(x.Id, x.InstitutionId, x.HolderId, x.Type, x.Agency, x.Number, x.CheckDigit, x.Currency, Money(x.OverdraftLimit), x.Active, x.Version);
    private static CreditCardEntry Card(CreditCard x) => new(x.Id, x.InstitutionId, x.BrandId, x.HolderId, x.Name, x.DueDay, x.ClosingDay, Money(x.CreditLimit), Money(x.CashLimit), Money(x.ForeignCashLimit), Money(x.PixLimit), Money(x.BillLimit), x.Active, x.Version, null);
    private static bool IsForeignKeyConflict(DbUpdateException exception) => exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } or SqlException { Number: 547 };
    private static bool IsUniqueConflict(DbUpdateException exception) => exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } or SqlException { Number: 2601 or 2627 };
}
