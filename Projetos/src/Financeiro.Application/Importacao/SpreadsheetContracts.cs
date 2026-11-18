namespace Financeiro.Application.Importacao;

public interface ISpreadsheetReader
{
    Task<IReadOnlyList<RawSheet>> ReadAsync(Stream workbook, CancellationToken cancellationToken = default);
}

public sealed record RawSheet(
    string Name,
    bool IsHidden,
    IReadOnlyList<RawRow> Rows);

public sealed record RawRow(
    int RowNumber,
    IReadOnlyList<RawCell> Cells);

public sealed record RawCell(
    string Address,
    int RowNumber,
    int ColumnNumber,
    string? Value,
    string? CachedValue,
    string? Formula,
    RawCellValueKind ValueKind,
    string? CommentText,
    string? CommentAuthor);

public enum RawCellValueKind
{
    Blank,
    Text,
    Number,
    Date,
    Boolean,
    Error,
    Formula
}
