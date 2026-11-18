namespace Financeiro.Application.Importacao;

public sealed class SpreadsheetOptions
{
    public const long DefaultMaxFileBytes = 10 * 1024 * 1024;
    public const int DefaultMaxSheets = 20;
    public const int DefaultMaxRowsPerSheet = 5_000;
    public const int DefaultMaxColumns = 200;
    public const int DefaultMaxCells = 500_000;
    public const int DefaultMaxIssues = 200;

    public long MaxFileBytes { get; init; } = DefaultMaxFileBytes;
    public int MaxSheets { get; init; } = DefaultMaxSheets;
    public int MaxRowsPerSheet { get; init; } = DefaultMaxRowsPerSheet;
    public int MaxColumns { get; init; } = DefaultMaxColumns;
    public int MaxCells { get; init; } = DefaultMaxCells;
    public int MaxIssues { get; init; } = DefaultMaxIssues;

    public void Validate()
    {
        if (MaxFileBytes <= 0) throw new Catalogs.CatalogValidationException("O limite de arquivo deve ser positivo.");
        if (MaxSheets <= 0) throw new Catalogs.CatalogValidationException("O limite de abas deve ser positivo.");
        if (MaxRowsPerSheet <= 0) throw new Catalogs.CatalogValidationException("O limite de linhas deve ser positivo.");
        if (MaxColumns <= 0) throw new Catalogs.CatalogValidationException("O limite de colunas deve ser positivo.");
        if (MaxCells <= 0) throw new Catalogs.CatalogValidationException("O limite de células deve ser positivo.");
        if (MaxIssues <= 0) throw new Catalogs.CatalogValidationException("O limite de ocorrências deve ser positivo.");
    }
}

public sealed class ImportValidationException(string message) : ArgumentException(message);
public sealed class ImportSizeException(string message) : Exception(message);
public sealed class ImportRuleException(string message) : Exception(message);
