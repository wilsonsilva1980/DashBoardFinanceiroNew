using Financeiro.Application.Importacao;

namespace Financeiro.Application.Tests;

internal static class SpreadsheetFixtures
{
    public static IReadOnlyList<RawSheet> AnnualAndInstallmentWorkbook() =>
    [
        new RawSheet("2026", true,
        [
            new RawRow(3, [Cell("A3", 1, "Dívida"), Cell("D3", 4, "Valor Total"), Cell("H3", 8, "Valor Total")]),
            new RawRow(4, [Cell("A4", 1, "Conta"), Cell("B4", 2, "10"), Cell("D4", 4, "100"), Cell("H4", 8, "200")]),
            new RawRow(5, [Cell("A5", 1, "Total"), Cell("D5", 4, "300")])
        ]),
        new RawSheet("Controle Pagamentos - 2026", false,
        [
            new RawRow(3, [Cell("A3", 1, "Dívida"), Cell("B3", 2, "Data Compra"), Cell("E3", 5, "Parcela")]),
            new RawRow(4, [Cell("A4", 1, "Compra"), Cell("E4", 5, "1"), Cell("F4", 6, "50")]),
            new RawRow(5, [])
        ]),
        new RawSheet("Formula", false,
        [
            new RawRow(2, [new RawCell("A2", 1, 2, "200", "200", "SUM(100,100)", RawCellValueKind.Formula, "revisar", "fixture")])
        ])
    ];

    private static RawCell Cell(string address, int column, string? value) =>
        new(address, 1, column, value, null, null,
            value is null ? RawCellValueKind.Blank : RawCellValueKind.Text, null, null);
}
