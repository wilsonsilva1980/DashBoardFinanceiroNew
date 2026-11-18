using Financeiro.Application.Importacao;
using Financeiro.Domain.Entities;
using Financeiro.Infrastructure.Importacao;

namespace Financeiro.Infrastructure.IntegrationTests;

public sealed class SpreadsheetReaderTests
{
    [Fact]
    public async Task ReadsVisibleAndHiddenSheetsWithoutRecalculatingFormulas()
    {
        await using var stream = File.OpenRead(Path.Combine(
            AppContext.BaseDirectory,
            "fixtures",
            "importacao",
            "representative-debt-workbook.xlsx"));

        var sheets = await new ClosedXmlSpreadsheetReader().ReadAsync(stream);

        Assert.Contains(sheets, sheet => sheet.IsHidden);
        var formulas = sheets.SelectMany(sheet => sheet.Rows).SelectMany(row => row.Cells)
            .Where(cell => cell.Formula is not null)
            .ToArray();
        Assert.NotEmpty(formulas);
        Assert.Contains(formulas, formula => formula.CachedValue == "200");
        Assert.All(formulas, formula => Assert.Equal(RawCellValueKind.Formula, formula.ValueKind));
        Assert.Contains(sheets.SelectMany(sheet => sheet.Rows).SelectMany(row => row.Cells),
            cell => cell.CommentText is not null);
    }

    [Fact]
    public async Task PropagatesCancellationBeforeOpeningWorkbook()
    {
        await using var stream = new MemoryStream();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new ClosedXmlSpreadsheetReader().ReadAsync(stream, cancellation.Token));
    }

    [Fact]
    public async Task ClassifiesHeadersAndTotalsWithoutMakingThemEligibleRecords()
    {
        await using var stream = File.OpenRead(Path.Combine(
            AppContext.BaseDirectory,
            "fixtures",
            "importacao",
            "representative-debt-workbook.xlsx"));
        var sheets = await new ClosedXmlSpreadsheetReader().ReadAsync(stream);

        var rows = sheets.SelectMany(SpreadsheetRowAnalyzer.Analyze).ToArray();

        Assert.Contains(rows, row => row.Classification == ImportRecordClass.Header);
        Assert.Contains(rows, row => row.Classification == ImportRecordClass.Total);
        Assert.Contains(rows, row => row.SourceType == ImportSourceType.Anticipation);

        var subtotal = SpreadsheetRowAnalyzer.Analyze(new RawSheet(
            "Parcelas",
            false,
            [new RawRow(1, [new RawCell("A1", 1, 1, "Subtotal", null, null, RawCellValueKind.Text, null, null)])]));
        Assert.Equal(ImportRecordClass.Subtotal, Assert.Single(subtotal).Classification);
    }

    [Fact]
    public async Task ReadsTheConsolidatedDebtWorkbookWithOfficeWebExtensionMetadata()
    {
        var workbookPath = Environment.GetEnvironmentVariable("TEST_WORKBOOK_PATH") ??
            Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "../../../../../../Guia/NovaPlanilhaDividas3.xlsx"));
        await using var stream = File.OpenRead(workbookPath);

        var sheets = await new ClosedXmlSpreadsheetReader().ReadAsync(stream);
        var rows = sheets.SelectMany(SpreadsheetRowAnalyzer.Analyze).ToArray();

        Assert.Equal(15, sheets.Count);
        Assert.Contains(rows, row => row.Classification == ImportRecordClass.Record);
    }

    [Fact]
    public void FlagsPaidMarkersAndNonEffectiveSourcesWithoutCreatingPayments()
    {
        var row = new ParsedSpreadsheetRow(
            "Antecipação",
            true,
            ImportSourceType.Anticipation,
            4,
            1,
            "A4",
            ImportRecordClass.Record,
            new Dictionary<string, string?> { ["divida"] = "Conta", ["pagamento"] = "P" },
            new Dictionary<string, string?> { ["status_origem"] = "P" });

        var issues = SpreadsheetIssueDetector.Detect(row);

        Assert.Contains(issues, issue => issue.Code == "fonte_nao_efetiva");
        Assert.Contains(issues, issue => issue.Code == "status_sem_evidencia");
    }
}
