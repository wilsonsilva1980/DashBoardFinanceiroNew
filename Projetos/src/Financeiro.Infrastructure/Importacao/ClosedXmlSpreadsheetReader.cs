using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Financeiro.Application.Importacao;
using System.IO.Compression;
using System.Xml.Linq;

namespace Financeiro.Infrastructure.Importacao;

public sealed class ClosedXmlSpreadsheetReader : ISpreadsheetReader
{
    public async Task<IReadOnlyList<RawSheet>> ReadAsync(
        Stream workbook,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workbook);
        if (!workbook.CanRead) throw new ImportValidationException("O arquivo não pode ser lido.");

        cancellationToken.ThrowIfCancellationRequested();
        await using var buffer = new MemoryStream();
        await workbook.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        RemoveOfficeWebExtensionMetadata(buffer);
        buffer.Position = 0;
        var comments = ReadComments(buffer);
        buffer.Position = 0;
        try
        {
            using var xlsx = new XLWorkbook(buffer);
            return ReadWithClosedXml(xlsx, comments, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            buffer.Position = 0;
            return ReadWithOpenXml(buffer, cancellationToken);
        }
    }

    private static IReadOnlyList<RawSheet> ReadWithClosedXml(
        XLWorkbook xlsx,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, RawComment>> comments,
        CancellationToken cancellationToken)
    {
        var sheets = new List<RawSheet>(xlsx.Worksheets.Count);
        foreach (var worksheet in xlsx.Worksheets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sheets.Add(ReadSheet(worksheet, comments.GetValueOrDefault(worksheet.Name), cancellationToken));
        }

        return sheets;
    }

    private static IReadOnlyList<RawSheet> ReadWithOpenXml(Stream workbook, CancellationToken cancellationToken)
    {
        using var document = SpreadsheetDocument.Open(workbook, false);
        var workbookPart = document.WorkbookPart ?? throw new ImportValidationException("A pasta de trabalho não contém um workbook.");
        var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable
            .Elements<SharedStringItem>()
            .Select(item => item.InnerText)
            .ToArray() ?? [];
        var sheets = new List<RawSheet>();

        foreach (var sheet in workbookPart.Workbook.Sheets?.Elements<Sheet>() ?? [])
        {
            cancellationToken.ThrowIfCancellationRequested();
            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id?.Value ?? throw new ImportValidationException("A aba não possui relacionamento."));
            var comments = worksheetPart.WorksheetCommentsPart?.Comments;
            var authors = comments?.Authors?.Elements<Author>().Select(author => author.InnerText).ToArray() ?? [];
            var commentByCell = new Dictionary<string, RawComment>(StringComparer.OrdinalIgnoreCase);
            foreach (var comment in comments?.CommentList?.Elements<Comment>() ?? [])
            {
                if (comment.Reference is null) continue;
                var reference = comment.Reference.Value;
                if (string.IsNullOrWhiteSpace(reference)) continue;
                var author = comment.AuthorId?.Value is uint value && value < authors.Length ? authors[value] : null;
                commentByCell[reference] = new RawComment(comment.InnerText, author);
            }
            var rows = new List<RawRow>();

            foreach (var row in worksheetPart.Worksheet.Descendants<Row>())
            {
                var cells = row.Elements<Cell>().Select(cell => ReadOpenXmlCell(cell, sharedStrings, commentByCell)).ToArray();
                rows.Add(new RawRow(Convert.ToInt32(row.RowIndex?.Value ?? 0u), cells));
            }

            sheets.Add(new RawSheet(
                sheet.Name?.Value ?? string.Empty,
                sheet.State?.Value == SheetStateValues.Hidden || sheet.State?.Value == SheetStateValues.VeryHidden,
                rows));
        }

        return sheets;
    }

    private static RawCell ReadOpenXmlCell(
        Cell cell,
        IReadOnlyList<string> sharedStrings,
        IReadOnlyDictionary<string, RawComment> comments)
    {
        var address = cell.CellReference?.Value ?? string.Empty;
        var (rowNumber, columnNumber) = ParseCellAddress(address);
        var formula = cell.CellFormula?.Text;
        var rawValue = cell.CellValue?.Text;
        var dataType = cell.DataType?.Value;
        string? value = rawValue;
        if (dataType == CellValues.SharedString &&
            int.TryParse(rawValue, out var index) && index >= 0 && index < sharedStrings.Count)
            value = sharedStrings[index];
        else if (dataType == CellValues.InlineString)
            value = cell.InlineString?.InnerText;
        else if (dataType == CellValues.Boolean)
            value = rawValue == "1" ? "TRUE" : "FALSE";

        var valueKind = formula is not null ? RawCellValueKind.Formula :
            dataType == CellValues.Boolean ? RawCellValueKind.Boolean :
            dataType == CellValues.Error ? RawCellValueKind.Error :
            dataType == CellValues.SharedString || dataType == CellValues.InlineString || dataType == CellValues.String
                ? RawCellValueKind.Text :
            string.IsNullOrWhiteSpace(value) ? RawCellValueKind.Blank : RawCellValueKind.Number;

        return new RawCell(
            address,
            rowNumber,
            columnNumber,
            value,
            formula is not null ? value : null,
            formula,
            valueKind,
            comments.GetValueOrDefault(address)?.Text,
            comments.GetValueOrDefault(address)?.Author);
    }

    private static (int Row, int Column) ParseCellAddress(string address)
    {
        var split = 0;
        while (split < address.Length && char.IsLetter(address[split])) split++;
        if (split == 0 || !int.TryParse(address[split..], out var row))
            throw new ImportValidationException($"A referência de célula é inválida: {address}.");

        var column = 0;
        foreach (var character in address[..split].ToUpperInvariant())
            column = checked(column * 26 + character - 'A' + 1);
        return (row, column);
    }

    private static void RemoveOfficeWebExtensionMetadata(Stream workbook)
    {
        (string FullName, byte[] Content)[] entries;
        byte[] contentTypesBytes;
        var changed = false;
        using (var archive = new ZipArchive(workbook, ZipArchiveMode.Read, leaveOpen: true))
        {
            var contentTypesEntry = archive.GetEntry("[Content_Types].xml");
            if (contentTypesEntry is null) return;

            XDocument contentTypes;
            using (var stream = contentTypesEntry.Open())
                contentTypes = XDocument.Load(stream);

            var root = contentTypes.Root;
            if (root is null) return;
            foreach (var webExtensionPart in archive.Entries
                         .Where(entry => entry.FullName.StartsWith("xl/webextensions/", StringComparison.OrdinalIgnoreCase)))
            {
                var partName = "/" + webExtensionPart.FullName;
                var expectedContentType = partName.EndsWith("taskpanes.xml", StringComparison.OrdinalIgnoreCase)
                    ? "application/vnd.ms-office.webextensiontaskpanes+xml"
                    : "application/vnd.ms-office.webextension+xml";
                var overrideElement = root.Elements()
                    .FirstOrDefault(element => element.Name.LocalName == "Override" &&
                        string.Equals((string?)element.Attribute("PartName"), partName, StringComparison.OrdinalIgnoreCase));
                if (overrideElement is null)
                {
                    root.Add(new XElement(root.GetDefaultNamespace() + "Override",
                        new XAttribute("PartName", partName),
                        new XAttribute("ContentType", expectedContentType)));
                    changed = true;
                }
                else if (!string.Equals((string?)overrideElement.Attribute("ContentType"), expectedContentType, StringComparison.Ordinal))
                {
                    overrideElement.SetAttributeValue("ContentType", expectedContentType);
                    changed = true;
                }
            }

            if (!changed) return;
            entries = archive.Entries
                .Select(entry =>
                {
                    using var stream = entry.Open();
                    using var copy = new MemoryStream();
                    stream.CopyTo(copy);
                    return (entry.FullName, Content: copy.ToArray());
                })
                .ToArray();
            using var normalizedContentTypes = new MemoryStream();
            contentTypes.Save(normalizedContentTypes);
            contentTypesBytes = normalizedContentTypes.ToArray();
        }

        workbook.Position = 0;
        workbook.SetLength(0);
        using (var normalizedArchive = new ZipArchive(workbook, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in entries)
            {
                var normalizedEntry = normalizedArchive.CreateEntry(entry.FullName);
                using var stream = normalizedEntry.Open();
                stream.Write(entry.FullName == "[Content_Types].xml"
                    ? contentTypesBytes
                    : entry.Content);
            }
        }
    }

    private static RawSheet ReadSheet(
        IXLWorksheet worksheet,
        IReadOnlyDictionary<string, RawComment>? comments,
        CancellationToken cancellationToken)
    {
        var firstRow = worksheet.FirstRowUsed(XLCellsUsedOptions.AllContents);
        var lastRow = worksheet.LastRowUsed(XLCellsUsedOptions.AllContents);
        var firstColumn = worksheet.FirstColumnUsed(XLCellsUsedOptions.AllContents);
        var lastColumn = worksheet.LastColumnUsed(XLCellsUsedOptions.AllContents);
        if (firstRow is null || lastRow is null || firstColumn is null || lastColumn is null)
            return new RawSheet(worksheet.Name, worksheet.Visibility != XLWorksheetVisibility.Visible, []);

        var rows = new List<RawRow>(lastRow.RowNumber() - firstRow.RowNumber() + 1);
        for (var rowNumber = firstRow.RowNumber(); rowNumber <= lastRow.RowNumber(); rowNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cells = new List<RawCell>(lastColumn.ColumnNumber() - firstColumn.ColumnNumber() + 1);
            for (var columnNumber = firstColumn.ColumnNumber(); columnNumber <= lastColumn.ColumnNumber(); columnNumber++)
            {
                var cell = worksheet.Cell(rowNumber, columnNumber);
                cells.Add(ReadCell(cell, comments?.GetValueOrDefault(cell.Address.ToString() ?? string.Empty)));
            }

            rows.Add(new RawRow(rowNumber, cells));
        }

        return new RawSheet(worksheet.Name, worksheet.Visibility != XLWorksheetVisibility.Visible, rows);
    }

    private static RawCell ReadCell(IXLCell cell, RawComment? rawComment)
    {
        var formula = string.IsNullOrWhiteSpace(cell.FormulaA1) ? null : cell.FormulaA1;
        var isFormula = formula is not null;
        var value = isFormula ? cell.CachedValue : cell.Value;
        var valueText = value.IsBlank ? null : value.ToString();
        var cachedText = isFormula && !cell.CachedValue.IsBlank ? cell.CachedValue.ToString() : null;
        return new RawCell(
            cell.Address.ToString() ?? string.Empty,
            cell.Address.RowNumber,
            cell.Address.ColumnNumber,
            valueText,
            cachedText,
            formula,
            isFormula ? RawCellValueKind.Formula : GetValueKind(value),
            rawComment?.Text,
            rawComment?.Author);
    }

    private static RawCellValueKind GetValueKind(XLCellValue value) =>
        value.IsBlank ? RawCellValueKind.Blank :
        value.IsText ? RawCellValueKind.Text :
        value.IsNumber ? RawCellValueKind.Number :
        value.IsDateTime || value.IsTimeSpan ? RawCellValueKind.Date :
        value.IsBoolean ? RawCellValueKind.Boolean :
        value.IsError ? RawCellValueKind.Error :
        RawCellValueKind.Text;

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, RawComment>> ReadComments(Stream workbook)
    {
        var result = new Dictionary<string, IReadOnlyDictionary<string, RawComment>>(StringComparer.Ordinal);
        using var archive = new ZipArchive(workbook, ZipArchiveMode.Read, leaveOpen: true);
        var workbookXml = ReadXml(archive, "xl/workbook.xml");
        var workbookRels = ReadXml(archive, "xl/_rels/workbook.xml.rels");
        var relationshipTargets = workbookRels.Root?.Elements()
            .Where(x => x.Name.LocalName == "Relationship")
            .ToDictionary(
                x => (string?)x.Attribute("Id") ?? string.Empty,
                x => ResolvePath("xl/workbook.xml", (string?)x.Attribute("Target") ?? string.Empty),
                StringComparer.Ordinal) ?? [];

        foreach (var sheet in workbookXml.Root?.Descendants().Where(x => x.Name.LocalName == "sheet") ?? [])
        {
            var name = (string?)sheet.Attribute("name");
            var relationshipId = (string?)sheet.Attributes().FirstOrDefault(x => x.Name.LocalName == "id");
            if (string.IsNullOrWhiteSpace(name) || relationshipId is null ||
                !relationshipTargets.TryGetValue(relationshipId, out var sheetPath))
                continue;

            var relPath = $"{Path.GetDirectoryName(sheetPath)?.Replace('\\', '/')}/_rels/{Path.GetFileName(sheetPath)}.rels";
            var relEntry = archive.GetEntry(relPath);
            if (relEntry is null) continue;
            var sheetRels = ReadXml(relEntry);
            var commentsTarget = sheetRels.Root?.Elements()
                .Where(x => x.Name.LocalName == "Relationship")
                .Where(x => ((string?)x.Attribute("Type"))?.EndsWith("/comments", StringComparison.Ordinal) == true)
                .Select(x => ResolvePath(sheetPath, (string?)x.Attribute("Target") ?? string.Empty))
                .FirstOrDefault();
            if (commentsTarget is null) continue;

            var commentsEntry = archive.GetEntry(commentsTarget);
            if (commentsEntry is null) continue;
            var commentsXml = ReadXml(commentsEntry);
            var authors = commentsXml.Root?.Descendants().Where(x => x.Name.LocalName == "author")
                .Select(x => x.Value).ToArray() ?? [];
            var sheetComments = new Dictionary<string, RawComment>(StringComparer.OrdinalIgnoreCase);
            foreach (var comment in commentsXml.Root?.Descendants().Where(x => x.Name.LocalName == "comment") ?? [])
            {
                var address = (string?)comment.Attribute("ref");
                if (string.IsNullOrWhiteSpace(address)) continue;
                var authorId = int.TryParse((string?)comment.Attribute("authorId"), out var parsed) ? parsed : -1;
                var author = authorId >= 0 && authorId < authors.Length ? authors[authorId] : null;
                var text = string.Concat(comment.Descendants().Where(x => x.Name.LocalName == "t").Select(x => x.Value));
                sheetComments[address] = new RawComment(text, author);
            }

            result[name] = sheetComments;
        }

        return result;
    }

    private static XDocument ReadXml(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path) ?? throw new ImportValidationException($"Parte ausente: {path}.");
        return ReadXml(entry);
    }

    private static XDocument ReadXml(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        return XDocument.Load(stream, System.Xml.Linq.LoadOptions.PreserveWhitespace);
    }

    private static string ResolvePath(string sourcePath, string target)
    {
        if (target.StartsWith("/", StringComparison.Ordinal))
            return target.TrimStart('/');

        var baseUri = new Uri($"https://workbook.invalid/{sourcePath}");
        return new Uri(baseUri, target).AbsolutePath.TrimStart('/');
    }

    private sealed record RawComment(string Text, string? Author);
}
