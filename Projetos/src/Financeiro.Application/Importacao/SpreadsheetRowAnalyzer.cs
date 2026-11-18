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
    IReadOnlyDictionary<string, string?> DerivedValues,
    int? Month = null,
    string OccurrenceKey = "",
    IReadOnlyDictionary<string, string?>? OriginalValues = null);

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

                var key = header;
                var suffix = 2;
                while (values.ContainsKey(key))
                    key = $"{header}_{suffix++}";
                values[key] = cell.Value;
            }
            var first = nonEmpty[0];
            if (classification != ImportRecordClass.Record)
            {
                parsed.Add(new ParsedSpreadsheetRow(
                    sheet.Name, sheet.IsHidden, sourceType, row.RowNumber, first.ColumnNumber,
                    first.Address, classification, values, new Dictionary<string, string?>()));
                continue;
            }

            var occurrences = ExpandMonthlyOccurrences(sheet.Name, sheet.IsHidden, sourceType, row, values);
            if (occurrences.Count == 0)
                occurrences.Add(new ParsedSpreadsheetRow(
                    sheet.Name, sheet.IsHidden, sourceType, row.RowNumber, first.ColumnNumber,
                    first.Address, classification, values, DeriveValues(values), null, string.Empty, values));
            parsed.AddRange(occurrences);
        }

        return parsed;
    }

    private static List<ParsedSpreadsheetRow> ExpandMonthlyOccurrences(
        string sheetName,
        bool isHidden,
        ImportSourceType sourceType,
        RawRow row,
        IReadOnlyDictionary<string, string?> rowValues)
    {
        if (sourceType is not (ImportSourceType.Annual or ImportSourceType.Installments or
            ImportSourceType.Anticipation or ImportSourceType.Backup or ImportSourceType.Scenario))
            return [];

        var result = new List<ParsedSpreadsheetRow>();
        var year = DetectYear(sheetName);
        for (var month = 1; month <= 12; month++)
        {
            var start = sourceType is ImportSourceType.Annual
                ? 4 + ((month - 1) * 4)
                : InstallmentStartColumn(sourceType, month);
            var width = sourceType is ImportSourceType.Annual ? 4 : InstallmentBlockWidth(sourceType, month);
            var cells = row.Cells.Where(cell => cell.ColumnNumber >= start && cell.ColumnNumber < start + width).ToArray();
            if (!cells.Any(cell => !string.IsNullOrWhiteSpace(cell.Value)))
                continue;

            var values = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (var pair in rowValues.Where(pair => !pair.Key.Contains('_', StringComparison.Ordinal)))
                values[pair.Key] = pair.Value;
            if (sourceType == ImportSourceType.Annual)
            {
                Put(values, "valortotal", cells.ElementAtOrDefault(0)?.Value);
                Put(values, "valorpago", cells.ElementAtOrDefault(1)?.Value);
                Put(values, "responsavel", cells.ElementAtOrDefault(2)?.Value);
                Put(values, "status", cells.ElementAtOrDefault(3)?.Value);
            }
            else
            {
                Put(values, "numeroparcela", cells.ElementAtOrDefault(0)?.Value);
                Put(values, "valorparcela", cells.ElementAtOrDefault(1)?.Value);
                Put(values, "valorfaltante", cells.ElementAtOrDefault(2)?.Value);
                Put(values, "valorpago", cells.ElementAtOrDefault(3)?.Value);
                Put(values, "responsavel", cells.ElementAtOrDefault(4)?.Value);
                Put(values, "status", cells.ElementAtOrDefault(5)?.Value);
                if (width > 6)
                {
                    Put(values, "valorquitacao", cells.ElementAtOrDefault(6)?.Value);
                    Put(values, "quitacaopago", cells.ElementAtOrDefault(7)?.Value);
                }
            }

            var derived = new Dictionary<string, string?>(DeriveValues(values), StringComparer.Ordinal);
            if (year is int actualYear)
                derived["competencia"] = $"{actualYear:0000}-{month:00}-01";
            var description = First(values, "divida", "descricao", "nome", "titulo") ?? string.Empty;
            var occurrenceKey = $"{sourceType}:{SpreadsheetTextNormalizer.Normalize(sheetName)}:{row.RowNumber}:{month}:{SpreadsheetTextNormalizer.Normalize(description)}";
            result.Add(new ParsedSpreadsheetRow(
                sheetName,
                isHidden,
                sourceType,
                row.RowNumber,
                start,
                cells[0].Address,
                ImportRecordClass.Record,
                values,
                derived,
                month,
                occurrenceKey,
                values));
        }

        return result;
    }

    private static int InstallmentStartColumn(ImportSourceType sourceType, int month)
    {
        var start = 5;
        for (var previous = 1; previous < month; previous++)
            start += InstallmentBlockWidth(sourceType, previous);
        return start;
    }

    private static int InstallmentBlockWidth(ImportSourceType sourceType, int month) =>
        sourceType == ImportSourceType.Anticipation && month == 4 ? 8 : 6;

    private static int? DetectYear(string sheetName)
    {
        var match = System.Text.RegularExpressions.Regex.Match(sheetName, @"(?<!\d)(20\d{2})(?!\d)");
        return match.Success && int.TryParse(match.Value, out var year) ? year : null;
    }

    private static void Put(IDictionary<string, string?> values, string key, string? value)
    {
        if (value is not null)
            values[key] = value;
    }

    private static string? First(IReadOnlyDictionary<string, string?> values, params string[] keys) =>
        keys.Select(key => values.TryGetValue(key, out var value) ? value : null)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    public static ImportSourceType DetectSourceType(string sheetName)
    {
        var normalized = SpreadsheetTextNormalizer.Normalize(sheetName);
        if (normalized.Contains("antecip")) return ImportSourceType.Anticipation;
        if (normalized.Contains("bkp") || normalized.Contains("backup")) return ImportSourceType.Backup;
        if (normalized.Contains("parcela") || normalized.Contains("fatura")) return ImportSourceType.Installments;
        if (normalized.Contains("recorr") || normalized.Contains("streaming") ||
            normalized.Contains("servico") || normalized.Contains("casa"))
            return ImportSourceType.Recurrence;
        if (normalized.Contains("viagem") || normalized.Contains("travel")) return ImportSourceType.Travel;
        if (normalized.Contains("cadastro") || normalized.Contains("catalog") ||
            normalized.Contains("contas") || normalized.Contains("banco") ||
            normalized.Contains("cartoes") || normalized.Contains("cartao"))
            return ImportSourceType.Catalog;
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
