using System.ComponentModel.DataAnnotations;

namespace Financeiro.Application.Catalogs;

public enum NamedCatalogKind { People, Categories, Institutions, CardBrands }

public sealed record PageRequest(int Page = 1, int PageSize = 25, string? Search = null, bool IncludeInactive = true)
{
    public int Offset()
    {
        if (Page < 1 || PageSize is < 1 or > 100 || (long)(Page - 1) * PageSize > int.MaxValue || Search?.Length > 200)
            throw new CatalogValidationException("Paginação inválida. Informe página positiva e até 100 itens por página.");
        return (Page - 1) * PageSize;
    }
}
public sealed record PageResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
public sealed record NamedCatalogInput([Required, MaxLength(200)] string Name, bool Active = true, long? Version = null, string? Nature = null, [MaxLength(20)] string? Code = null);
public sealed record NamedCatalogEntry(Guid Id, string Name, bool Active, long Version, string? Nature, string? Code);
public sealed record BankAccountInput(Guid InstitutionId, Guid? HolderId, [MaxLength(30)] string? Type, [MaxLength(30)] string? Agency, [MaxLength(60)] string? Number, [MaxLength(10)] string? CheckDigit, [Required] string Currency, decimal OverdraftLimit, bool Active = true, long? Version = null);
public sealed record BankAccountEntry(Guid Id, Guid InstitutionId, Guid? HolderId, string? Type, string? Agency, string? Number, string? CheckDigit, string Currency, string OverdraftLimit, bool Active, long Version);
public sealed record CreditCardInput(Guid InstitutionId, Guid? BrandId, Guid? HolderId, [Required, MaxLength(120)] string Name, int? DueDay, int? ClosingDay, decimal? CreditLimit, decimal? CashLimit, decimal? ForeignCashLimit, decimal? PixLimit, decimal? BillLimit, bool Active = true, long? Version = null);
public sealed record CreditCardEntry(Guid Id, Guid InstitutionId, Guid? BrandId, Guid? HolderId, string Name, int? DueDay, int? ClosingDay, string? CreditLimit, string? CashLimit, string? ForeignCashLimit, string? PixLimit, string? BillLimit, bool Active, long Version, string? AvailableCredit);

public sealed class CatalogValidationException(string message) : ArgumentException(message);
public sealed class CatalogNotFoundException() : Exception("Registro não encontrado no espaço ativo.");
public sealed class ReferencedCatalogException() : Exception("Este cadastro está em uso. Desative-o para preservar o histórico financeiro.");
public sealed class CatalogNameConflictException() : Exception("Já existe uma bandeira com este nome no espaço ativo.");

public interface ICatalogService
{
    Task<PageResult<NamedCatalogEntry>> ListAsync(NamedCatalogKind kind, PageRequest page, CancellationToken cancellationToken = default);
    Task<NamedCatalogEntry> GetAsync(NamedCatalogKind kind, Guid id, CancellationToken cancellationToken = default);
    Task<NamedCatalogEntry> CreateAsync(NamedCatalogKind kind, NamedCatalogInput input, CancellationToken cancellationToken = default);
    Task<NamedCatalogEntry> UpdateAsync(NamedCatalogKind kind, Guid id, NamedCatalogInput input, CancellationToken cancellationToken = default);
    Task DeleteAsync(NamedCatalogKind kind, Guid id, long version, CancellationToken cancellationToken = default);
    Task<PageResult<BankAccountEntry>> AccountsAsync(PageRequest page, CancellationToken cancellationToken = default);
    Task<BankAccountEntry> AccountAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BankAccountEntry> SaveAccountAsync(Guid? id, BankAccountInput input, CancellationToken cancellationToken = default);
    Task DeleteAccountAsync(Guid id, long version, CancellationToken cancellationToken = default);
    Task<PageResult<CreditCardEntry>> CardsAsync(PageRequest page, CancellationToken cancellationToken = default);
    Task<CreditCardEntry> CardAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CreditCardEntry> SaveCardAsync(Guid? id, CreditCardInput input, CancellationToken cancellationToken = default);
    Task DeleteCardAsync(Guid id, long version, CancellationToken cancellationToken = default);
}

public static class CatalogValidation
{
    public static string Name(string? name, int limit = 200)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > limit) throw new CatalogValidationException("Informe um nome válido.");
        return name.Trim();
    }
    public static void Version(long? expected, long actual)
    {
        if (expected is null or <= 0) throw new CatalogValidationException("Informe a versão atual do cadastro.");
        if (expected != actual) throw new Financeiro.Application.Access.RequestConflictException("O cadastro foi alterado. Recarregue antes de salvar.");
    }
    public static void Money(decimal? value)
    {
        if (value.HasValue && (value < 0 || value > 99999999999999999.99m || decimal.Round(value.Value, 2) != value))
            throw new CatalogValidationException("Informe um valor não negativo com até duas casas decimais.");
    }
    public static void Day(int? day)
    {
        if (day is < 1 or > 31) throw new CatalogValidationException("O dia deve estar entre 1 e 31.");
    }
    public static void Currency(string? currency)
    {
        if (!new[] { "BRL", "USD", "EUR", "CLP", "ARS", "GBP", "JPY", "CAD", "AUD", "CHF", "CNY", "UYU", "PYG", "BOB", "PEN", "COP", "MXN" }.Contains(currency))
            throw new CatalogValidationException("Moeda não suportada.");
    }
    public static void Text(string? value, int limit)
    {
        if (value?.Length > limit) throw new CatalogValidationException("Um identificador excedeu o tamanho permitido.");
    }
}
