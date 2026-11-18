using Financeiro.Domain.Entities;

namespace Financeiro.Application.Importacao;

public sealed record ParsedSpreadsheetRow(
    string SheetName,
    bool IsHidden,
    ImportSourceType SourceType,
    int RowNumber,
    int? StartColumn,
    string CellReference,
    ImportRecordClass Classification,
    IReadOnlyDictionary<string, string?> Values,
    IReadOnlyDictionary<string, string?> DerivedValues);

public static class SpreadsheetRowAnalyzer
{
    public static IReadOnlyList<ParsedSpreadsheetRow> Analyze(RawSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        var parsed = new List<ParsedSpreadsheetRow>(sheet.Rows.Count);
        var headers = new Dictionary<int, string>();
        var sourceType = DetectSourceType(sheet.Name);
        foreach (var row in sheet.Rows)
        {
            var nonEmpty = row.Cells.Where(cell => !string.IsNullOrWhiteSpace(cell.Value)).ToArray();
            var classification = Classify(nonEmpty);
            if (classification == ImportRecordClass.Header)
            {
                headers = nonEmpty.ToDictionary(
                    cell => cell.ColumnNumber,
                    cell => SpreadsheetTextNormalizer.NormalizeHeader(cell.Value));
            }

            if (nonEmpty.Length == 0) continue;
            var values = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (var cell in row.Cells)
            {
                if (!headers.TryGetValue(cell.ColumnNumber, out var header) ||
                    string.IsNullOrWhiteSpace(header))
                    continue;

                if (!values.TryGetValue(header, out var current) ||
                    string.IsNullOrWhiteSpace(current))
                    values[header] = cell.Value;
            }
            var derived = classification == ImportRecordClass.Record
                ? DeriveValues(values)
                : new Dictionary<string, string?>();
            var first = nonEmpty[0];
            parsed.Add(new ParsedSpreadsheetRow(
                sheet.Name,
                sheet.IsHidden,
                sourceType,
                row.RowNumber,
                first.ColumnNumber,
                first.Address,
                classification,
                values,
                derived));
        }

        return parsed;
    }

    public static ImportSourceType DetectSourceType(string sheetName)
    {
        var normalized = SpreadsheetTextNormalizer.Normalize(sheetName);
        if (normalized.Contains("antecip")) return ImportSourceType.Anticipation;
        if (normalized.Contains("bkp") || normalized.Contains("backup")) return ImportSourceType.Backup;
        if (normalized.Contains("parcela") || normalized.Contains("fatura")) return ImportSourceType.Installments;
        if (normalized.Contains("recorr")) return ImportSourceType.Recurrence;
        if (normalized.Contains("viagem") || normalized.Contains("travel")) return ImportSourceType.Travel;
        if (normalized.Contains("cadastro") || normalized.Contains("catalog")) return ImportSourceType.Catalog;
        if (normalized.Contains("2027") || normalized.Contains("cenario") || normalized.Contains("scenario"))
            return ImportSourceType.Scenario;
        return ImportSourceType.Annual;
    }

    private static ImportRecordClass Classify(IReadOnlyList<RawCell> cells)
    {
        if (cells.Count == 0) return ImportRecordClass.Section;
        var headers = cells.Select(cell => SpreadsheetTextNormalizer.NormalizeHeader(cell.Value)).ToHashSet();
        if (headers.Contains("divida") || headers.Contains("datavencimento") ||
               headers.Contains("valortotal") || headers.Contains("pagamento"))
            return ImportRecordClass.Header;
        var firstCell = SpreadsheetTextNormalizer.Normalize(cells[0].Value);
        if (firstCell == "subtotal" || firstCell.StartsWith("subtotal", StringComparison.Ordinal))
            return ImportRecordClass.Subtotal;
        if (firstCell == "total" || firstCell.StartsWith("totalgeral", StringComparison.Ordinal))
            return ImportRecordClass.Total;
        var text = string.Join(' ', cells.Select(cell => SpreadsheetTextNormalizer.Normalize(cell.Value)));
        if (text.Contains("subtotal")) return ImportRecordClass.Subtotal;
        if (text == "total" || text.StartsWith("totalgeral", StringComparison.Ordinal))
            return ImportRecordClass.Total;
        if (text.Contains("modelo") || text.Contains("template")) return ImportRecordClass.Model;
        if (cells.Count == 1 && cells[0].ValueKind == RawCellValueKind.Text) return ImportRecordClass.Section;
        return ImportRecordClass.Record;
    }

    private static IReadOnlyDictionary<string, string?> DeriveValues(IReadOnlyDictionary<string, string?> values)
    {
        var result = new Dictionary<string, string?>(StringComparer.Ordinal);
        Copy(values, result, "competencia", "competencia", "mes", "data", "datavencimento");
        Copy(values, result, "numero_parcela", "numeroparcela", "parcela", "nparcela");
        Copy(values, result, "valor_original", "valortotal", "valorparcela", "valor");
        Copy(values, result, "responsavel", "responsavel", "quem");
        Copy(values, result, "status_origem", "status", "pagamento", "pago");
        return result;
    }

    private static void Copy(
        IReadOnlyDictionary<string, string?> source,
        IDictionary<string, string?> target,
        string destination,
        params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (source.TryGetValue(candidate, out var value))
            {
                target[destination] = value;
                return;
            }
        }
    }
}
