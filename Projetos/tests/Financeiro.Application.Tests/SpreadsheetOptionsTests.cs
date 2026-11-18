using Financeiro.Application.Catalogs;
using Financeiro.Application.Importacao;

namespace Financeiro.Application.Tests;

public sealed class SpreadsheetOptionsTests
{
    [Fact]
    public void DefaultsMatchTheImportSafetyBudget()
    {
        var options = new SpreadsheetOptions();

        Assert.Equal(10 * 1024 * 1024, options.MaxFileBytes);
        Assert.Equal(20, options.MaxSheets);
        Assert.Equal(5_000, options.MaxRowsPerSheet);
        Assert.Equal(200, options.MaxColumns);
        Assert.Equal(500_000, options.MaxCells);
        Assert.Equal(200, options.MaxIssues);
        options.Validate();
    }

    [Theory]
    [InlineData(nameof(SpreadsheetOptions.MaxFileBytes))]
    [InlineData(nameof(SpreadsheetOptions.MaxSheets))]
    [InlineData(nameof(SpreadsheetOptions.MaxRowsPerSheet))]
    [InlineData(nameof(SpreadsheetOptions.MaxColumns))]
    [InlineData(nameof(SpreadsheetOptions.MaxCells))]
    [InlineData(nameof(SpreadsheetOptions.MaxIssues))]
    public void EveryLimitMustBePositive(string limit)
    {
        var options = limit switch
        {
            nameof(SpreadsheetOptions.MaxFileBytes) => new SpreadsheetOptions { MaxFileBytes = 0 },
            nameof(SpreadsheetOptions.MaxSheets) => new SpreadsheetOptions { MaxSheets = 0 },
            nameof(SpreadsheetOptions.MaxRowsPerSheet) => new SpreadsheetOptions { MaxRowsPerSheet = 0 },
            nameof(SpreadsheetOptions.MaxColumns) => new SpreadsheetOptions { MaxColumns = 0 },
            nameof(SpreadsheetOptions.MaxCells) => new SpreadsheetOptions { MaxCells = 0 },
            nameof(SpreadsheetOptions.MaxIssues) => new SpreadsheetOptions { MaxIssues = 0 },
            _ => throw new ArgumentOutOfRangeException(nameof(limit))
        };

        Assert.Throws<CatalogValidationException>(options.Validate);
    }

    [Fact]
    public void ImportSpecificFailuresRemainDistinguishable()
    {
        Assert.IsType<ImportSizeException>(new ImportSizeException("413"));
        Assert.IsType<ImportRuleException>(new ImportRuleException("422"));
    }
}
