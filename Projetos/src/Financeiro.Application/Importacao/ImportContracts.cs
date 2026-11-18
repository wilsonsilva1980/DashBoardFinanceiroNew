using Financeiro.Domain.Entities;
using Financeiro.Application.Catalogs;

namespace Financeiro.Application.Importacao;

public sealed record ImportAnalysisResult(
    Guid BatchId,
    ImportState State,
    int TotalRows,
    int EligibleRows,
    int ErrorCount,
    int WarningCount,
    int InfoCount);
public sealed record ImportBatchEntry(
    Guid Id,
    string OriginalFileName,
    long SizeBytes,
    string ParserVersion,
    ImportState State,
    string StatisticsJson,
    long Version,
    DateTimeOffset CreatedAt);

public sealed record ImportRowFilter(
    int Page = 1,
    int PageSize = 25,
    string? SheetName = null,
    ImportIssueSeverity? Severity = null,
    ImportDecision? Decision = null)
{
    public int Offset()
    {
        if (Page < 1 || PageSize is < 1 or > 100)
            throw new ImportValidationException("Paginação de importação inválida.");
        return checked((Page - 1) * PageSize);
    }
}

public sealed record ImportRowEntry(
    Guid Id,
    string SheetName,
    int RowNumber,
    string CellReference,
    ImportSourceType SourceType,
    ImportRecordClass Classification,
    string DerivedValuesJson,
    string? IssueCode,
    string? IssueMessage,
    ImportIssueSeverity Severity,
    ImportDecision Decision,
    long Version);

public sealed record ImportPendingSummary(int Errors, int Warnings, int Pending);
public sealed record ImportDecisionInput(ImportDecision Decision, long Version);
public sealed record ImportConfirmationResult(
    Guid BatchId,
    ImportState State,
    int Created,
    int Reused,
    int Rejected,
    int Failed);

public interface IImportService
{
    Task<ImportBatchEntry> GetAsync(Guid batchId, CancellationToken cancellationToken = default);
    Task<ImportAnalysisResult> AnalyzeAsync(
        Stream workbook,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default);
    Task<PageResult<ImportRowEntry>> ListRowsAsync(
        Guid batchId,
        ImportRowFilter filter,
        CancellationToken cancellationToken = default);
    Task<byte[]> ExportCsvAsync(Guid batchId, ImportRowFilter filter, CancellationToken cancellationToken = default);
    Task<ImportPendingSummary> PendingAsync(Guid batchId, CancellationToken cancellationToken = default);
    Task<ImportRowEntry> DecideAsync(
        Guid batchId,
        Guid rowId,
        ImportDecisionInput input,
        CancellationToken cancellationToken = default);
    Task<ImportConfirmationResult> ConfirmAsync(
        Guid batchId,
        long version,
        CancellationToken cancellationToken = default);
    Task CancelAsync(Guid batchId, long version, CancellationToken cancellationToken = default);
}
