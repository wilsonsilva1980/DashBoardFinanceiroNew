namespace Financeiro.Domain.Entities;

public sealed class ImportSourceRecord : ScopedEntity
{
    public Guid ImportBatchId { get; set; }
    public string SheetName { get; set; } = string.Empty;
    public string NormalizedSheetName { get; set; } = string.Empty;
    public ImportSourceType SourceType { get; set; }
    public int RowNumber { get; set; }
    public int? StartColumn { get; set; }
    public string CellReference { get; set; } = string.Empty;
    public ImportRecordClass Classification { get; set; } = ImportRecordClass.Record;
    public string RawContentJson { get; set; } = "{}";
    public string DerivedValuesJson { get; set; } = "{}";
    public string? IssueCode { get; set; }
    public string? IssueMessage { get; set; }
    public ImportIssueSeverity Severity { get; set; } = ImportIssueSeverity.Info;
    public ImportDecision Decision { get; set; } = ImportDecision.Pending;
    public Guid? DecidedBy { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public string? PrevailingSource { get; set; }
}

public enum ImportSourceType { Annual, Installments, Anticipation, Backup, Catalog, Recurrence, Travel, Scenario }
public enum ImportRecordClass { Record, Header, Section, Subtotal, Total, Model }
public enum ImportIssueSeverity { Error, Warning, Info }
public enum ImportDecision { Pending, Approved, Rejected, Ignored }
