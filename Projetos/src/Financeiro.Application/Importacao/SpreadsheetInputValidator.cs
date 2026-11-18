using System.IO.Compression;
using System.Xml.Linq;

namespace Financeiro.Application.Importacao;

public static class SpreadsheetInputValidator
{
    public const int DefaultMaxZipEntries = 1_000;

    public static async Task<MemoryStream> ValidateAsync(
        Stream input,
        string fileName,
        string? contentType,
        SpreadsheetOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        if (string.IsNullOrWhiteSpace(fileName) ||
            !fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new ImportValidationException("Apenas arquivos .xlsx são aceitos.");
        if (!string.Equals(contentType, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", StringComparison.OrdinalIgnoreCase))
            throw new ImportValidationException("O tipo MIME do arquivo não é suportado.");

        var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length > options.MaxFileBytes)
            throw new ImportSizeException("O arquivo excede o limite configurado.");
        buffer.Position = 0;

        try
        {
            using var archive = new ZipArchive(buffer, ZipArchiveMode.Read, leaveOpen: true);
            if (archive.Entries.Count > DefaultMaxZipEntries)
                throw new ImportSizeException("O arquivo contém entradas ZIP demais.");

            long uncompressedBytes = 0;
            var worksheetCount = 0;
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (entry.FullName.EndsWith("vbaProject.bin", StringComparison.OrdinalIgnoreCase))
                    throw new ImportValidationException("Arquivos .xlsx com macros não são aceitos.");
                uncompressedBytes = checked(uncompressedBytes + entry.Length);
                if (uncompressedBytes > options.MaxFileBytes * 5)
                    throw new ImportSizeException("O tamanho descomprimido excede o limite configurado.");
                if (!entry.FullName.StartsWith("xl/worksheets/", StringComparison.OrdinalIgnoreCase) ||
                    !entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    continue;

                worksheetCount++;
                if (worksheetCount > options.MaxSheets)
                    throw new ImportSizeException("O arquivo contém abas demais.");
                ValidateDeclaredDimension(entry, options);
            }
        }
        catch (InvalidDataException ex)
        {
            throw new ImportValidationException($"O arquivo não é um ZIP .xlsx válido: {ex.Message}");
        }
        catch (OverflowException)
        {
            throw new ImportSizeException("O tamanho descomprimido excede o limite configurado.");
        }

        buffer.Position = 0;
        return buffer;
    }

    private static void ValidateDeclaredDimension(ZipArchiveEntry entry, SpreadsheetOptions options)
    {
        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        var dimension = document.Descendants().FirstOrDefault(x => x.Name.LocalName == "dimension");
        var reference = (string?)dimension?.Attribute("ref");
        if (string.IsNullOrWhiteSpace(reference)) return;

        var parts = reference.Split(':', 2);
        var start = ParseCellReference(parts[0]);
        var end = ParseCellReference(parts.Length == 2 ? parts[1] : parts[0]);
        var rows = end.Row - start.Row + 1;
        var columns = end.Column - start.Column + 1;
        if (rows > options.MaxRowsPerSheet)
            throw new ImportSizeException("A aba excede o limite de linhas.");
        if (columns > options.MaxColumns)
            throw new ImportSizeException("A aba excede o limite de colunas.");
        if ((long)rows * columns > options.MaxCells)
            throw new ImportSizeException("A aba excede o limite de células.");
    }

    private static (int Row, int Column) ParseCellReference(string value)
    {
        var split = 0;
        while (split < value.Length && char.IsLetter(value[split])) split++;
        if (split == 0 || split == value.Length ||
            !int.TryParse(value[split..], out var row) || row <= 0)
            throw new ImportValidationException("A dimensão declarada da aba é inválida.");

        var column = 0;
        foreach (var character in value[..split].ToUpperInvariant())
            column = checked(column * 26 + character - 'A' + 1);
        return (row, column);
    }
}
