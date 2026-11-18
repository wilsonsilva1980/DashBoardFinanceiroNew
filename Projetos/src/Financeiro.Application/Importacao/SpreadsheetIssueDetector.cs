using Financeiro.Domain.Entities;

namespace Financeiro.Application.Importacao;

public sealed record SpreadsheetIssue(
    string Code,
    string Message,
    ImportIssueSeverity Severity);

public static class SpreadsheetIssueDetector
{
    public static IReadOnlyList<SpreadsheetIssue> Detect(ParsedSpreadsheetRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        if (row.Classification != ImportRecordClass.Record) return [];

        var issues = new List<SpreadsheetIssue>();
        if (row.SourceType is ImportSourceType.Backup or ImportSourceType.Scenario or ImportSourceType.Anticipation)
            issues.Add(new("fonte_nao_efetiva", "A fonte será mantida como previsão, cenário ou reconciliação.", ImportIssueSeverity.Warning));

        if (row.DerivedValues.TryGetValue("status_origem", out var status) &&
            (string.Equals(status?.Trim(), "P", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(status?.Trim(), "Pago", StringComparison.OrdinalIgnoreCase)))
        {
            issues.Add(new("status_sem_evidencia", "O marcador de pago exige revisão e não cria pagamento bancário.", ImportIssueSeverity.Warning));
        }

        if (row.SourceType == ImportSourceType.Travel &&
            (!row.Values.TryGetValue("moeda", out var currency) || string.IsNullOrWhiteSpace(currency)))
            issues.Add(new("moeda_ambigua", "A moeda da viagem não foi informada.", ImportIssueSeverity.Error));

        return issues;
    }

    public static IReadOnlyList<SpreadsheetIssue> DetectDuplicates(IEnumerable<ParsedSpreadsheetRow> rows)
    {
        var groups = rows
            .Where(row => row.Classification == ImportRecordClass.Record)
            .GroupBy(row => BuildOccurrenceKey(row), StringComparer.Ordinal)
            .Where(group => group.Key.Length > 0 && group.Skip(1).Any());
        return groups
            .Select(group => new SpreadsheetIssue(
                "registro_duplicado",
                $"A ocorrência aparece {group.Count()} vezes na origem.",
                ImportIssueSeverity.Warning))
            .ToArray();
    }

    private static string BuildOccurrenceKey(ParsedSpreadsheetRow row)
    {
        var description = row.Values.GetValueOrDefault("divida") ?? string.Empty;
        var date = row.DerivedValues.GetValueOrDefault("competencia") ??
                   row.Values.GetValueOrDefault("datavencimento") ?? string.Empty;
        var value = row.DerivedValues.GetValueOrDefault("valor_original") ?? string.Empty;
        return $"{SpreadsheetTextNormalizer.Normalize(description)}|{date}|{value}";
    }
}
