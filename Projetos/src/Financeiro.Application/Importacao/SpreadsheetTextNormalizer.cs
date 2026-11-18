using System.Globalization;
using System.Text;

namespace Financeiro.Application.Importacao;

public static class SpreadsheetTextNormalizer
{
    private static readonly IReadOnlyDictionary<string, string> HeaderAliases =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["agamento"] = "pagamento",
            ["valorago"] = "valorquitacao",
            ["qtoagarei"] = "qtopagarei",
            ["qtopagarei"] = "qtopagarei",
            ["datavenc"] = "datavencimento",
            ["divida"] = "divida",
            ["valortotal"] = "valortotal"
        };

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(character)) builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }

    public static string NormalizeHeader(string? value)
    {
        var normalized = Normalize(value);
        return HeaderAliases.TryGetValue(normalized, out var canonical) ? canonical : normalized;
    }

    public static bool TryParseDate(string? value, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var serial) &&
            serial >= 32 && serial <= 100_000)
        {
            try
            {
                date = DateOnly.FromDateTime(DateTime.FromOADate((double)serial));
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        return DateOnly.TryParse(
            value,
            CultureInfo.GetCultureInfo("pt-BR"),
            DateTimeStyles.AllowWhiteSpaces,
            out date);
    }

    public static bool IsIsolatedDay(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var day) &&
        day is >= 1 and <= 31;
}
