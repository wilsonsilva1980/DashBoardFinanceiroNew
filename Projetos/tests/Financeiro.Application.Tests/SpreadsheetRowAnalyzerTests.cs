using Financeiro.Application.Importacao;
using Financeiro.Domain.Entities;

namespace Financeiro.Application.Tests;

public sealed class SpreadsheetRowAnalyzerTests
{
    [Fact]
    public void FixturePreservesHiddenSheetsTotalsBlankRowsAndFormulaEvidence()
    {
        var fixture = SpreadsheetFixtures.AnnualAndInstallmentWorkbook();

        Assert.Contains(fixture, sheet => sheet.IsHidden);
        Assert.Contains(fixture.SelectMany(sheet => sheet.Rows), row => row.Cells.Count == 0);
        Assert.Contains(fixture.SelectMany(sheet => sheet.Rows).SelectMany(row => row.Cells),
            cell => cell.ValueKind == RawCellValueKind.Formula && cell.CachedValue == "200");
        Assert.Contains(fixture.SelectMany(sheet => sheet.Rows).SelectMany(row => row.Cells),
            cell => cell.CommentText == "revisar");
    }

    [Fact]
    public void ProducesOneAnnualOccurrencePerPopulatedMonth()
    {
        var header = new RawRow(4, [
            Cell("A4", 1, "Dívida"), Cell("B4", 2, "Data - Venc."),
            Cell("C4", 3, "Limite de Crédito"), Cell("D4", 4, "Valor Total"),
            Cell("E4", 5, "Valor Pago"), Cell("F4", 6, "Pagamento"), Cell("G4", 7, "Status"),
            Cell("H4", 8, "Valor Total"), Cell("I4", 9, "Valor Pago"), Cell("J4", 10, "Pagamento"),
            Cell("K4", 11, "Status")
        ]);
        var row = new RawRow(7, [
            Cell("A7", 1, "Conta"), Cell("B7", 2, "10"), Cell("D7", 4, "100"),
            Cell("F7", 6, "Ana"), Cell("H7", 8, "200"), Cell("J7", 10, "Ana")
        ]);

        var parsed = SpreadsheetRowAnalyzer.Analyze(new RawSheet("2026", false, [header, row]));

        var occurrences = parsed.Where(x => x.Classification == ImportRecordClass.Record).ToArray();
        Assert.Equal(2, occurrences.Length);
        Assert.Equal([1, 2], occurrences.Select(x => x.Month));
        Assert.Equal(["100", "200"], occurrences.Select(x => x.DerivedValues["valor_original"]));
        Assert.Equal(["2026-01-01", "2026-02-01"], occurrences.Select(x => x.DerivedValues["competencia"]));
    }

    [Fact]
    public void KeepsTheEightFieldAprilAnticipationBlockAligned()
    {
        var header = new RawRow(4, [
            Cell("A4", 1, "Dívida"), Cell("B4", 2, "Data Compra"),
            Cell("C4", 3, "Qtde Parcelas"), Cell("D4", 4, "Data - Venc.")
        ]);
        var cells = new List<RawCell> { Cell("A7", 1, "Compra"), Cell("D7", 4, "10") };
        for (var month = 1; month <= 12; month++)
        {
            var start = 5 + Enumerable.Range(1, month - 1).Sum(previous => previous == 4 ? 8 : 6);
            var width = month == 4 ? 8 : 6;
            cells.Add(Cell($"A{start}7", start, "1"));
            cells.Add(Cell($"A{start + 1}7", start + 1, month == 4 ? "400" : "100"));
            for (var column = start + 2; column < start + width; column++)
                cells.Add(Cell($"A{column}7", column, null));
        }

        var parsed = SpreadsheetRowAnalyzer.Analyze(new RawSheet(
            "CP - 2026 - Antecipação de Parc", false, [header, new RawRow(7, cells)]));
        var april = Assert.Single(parsed, x => x.Month == 4);

        Assert.Equal("400", april.DerivedValues["valor_original"]);
        Assert.Equal(23, april.StartColumn);
    }

    private static RawCell Cell(string address, int column, string? value) =>
        new(address, 1, column, value, null, null,
            value is null ? RawCellValueKind.Blank : RawCellValueKind.Text, null, null);
}
