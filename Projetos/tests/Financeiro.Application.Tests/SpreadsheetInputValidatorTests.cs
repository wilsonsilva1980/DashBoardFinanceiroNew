using Financeiro.Application.Importacao;

namespace Financeiro.Application.Tests;

public sealed class SpreadsheetInputValidatorTests
{
    [Fact]
    public void NormalizesAccentsPunctuationAndKnownHeaderTypos()
    {
        Assert.Equal("pagamento", SpreadsheetTextNormalizer.NormalizeHeader("Agamento"));
        Assert.Equal("valorquitacao", SpreadsheetTextNormalizer.NormalizeHeader("Valor ago"));
        Assert.Equal("qtopagarei", SpreadsheetTextNormalizer.NormalizeHeader("Qto. agarei"));
    }

    [Fact]
    public void ParsesSerialDatesButKeepsAnIsolatedDayAmbiguous()
    {
        Assert.True(SpreadsheetTextNormalizer.TryParseDate("45292", out var date));
        Assert.Equal(new DateOnly(2024, 1, 1), date);
        Assert.True(SpreadsheetTextNormalizer.IsIsolatedDay("31"));
        Assert.False(SpreadsheetTextNormalizer.TryParseDate("31", out _));
    }

    [Fact]
    public async Task RejectsWrongExtensionAndMimeBeforeReading()
    {
        await Assert.ThrowsAsync<ImportValidationException>(() =>
            SpreadsheetInputValidator.ValidateAsync(
                new MemoryStream(),
                "debitos.xls",
                "application/vnd.ms-excel",
                new SpreadsheetOptions()));
    }

    [Fact]
    public async Task RejectsFileAboveConfiguredLimit()
    {
        await Assert.ThrowsAsync<ImportSizeException>(() =>
            SpreadsheetInputValidator.ValidateAsync(
                new MemoryStream(new byte[11]),
                "debitos.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                new SpreadsheetOptions { MaxFileBytes = 10 }));
    }

    [Fact]
    public async Task AcceptsRepresentativeWorkbookAndRejectsMalformedZip()
    {
        await using var fixture = File.OpenRead(Path.Combine(
            AppContext.BaseDirectory,
            "fixtures",
            "importacao",
            "representative-debt-workbook.xlsx"));

        await using var validated = await SpreadsheetInputValidator.ValidateAsync(
            fixture,
            "debitos.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            new SpreadsheetOptions());
        Assert.True(validated.Length > 0);

        await Assert.ThrowsAsync<ImportValidationException>(() =>
            SpreadsheetInputValidator.ValidateAsync(
                new MemoryStream([1, 2, 3]),
                "debitos.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                new SpreadsheetOptions()));
    }
}
