namespace Financeiro.Domain.Entities;

public sealed class ImportSourceRecord : ScopedEntity
{
    public Guid ImportBatchId { get; set; }
    public string SheetName { get; set; } = string.Empty;
    public string NormalizedSheetName { get; set; } = string.Empty;
    public ImportSourceType SourceType { get; set; }
    public int RowNumber { get; set; }
    public int? StartColumn { get; set; }
    public int? Month { get; set; }
    public string CellReference { get; set; } = string.Empty;
    public string OccurrenceKey { get; set; } = string.Empty;
    public ImportRecordClass Classification { get; set; } = ImportRecordClass.Record;
    public string RawContentJson { get; set; } = "{}";
    public string OriginalValuesJson { get; set; } = "{}";
    public string DerivedValuesJson { get; set; } = "{}";
    public string? IssueCode { get; set; }
    public string? IssueMessage { get; set; }
    public ImportIssueSeverity Severity { get; set; } = ImportIssueSeverity.Info;
    public ImportDecision Decision { get; set; } = ImportDecision.Pending;
    public Guid? DecidedBy { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public string? PrevailingSource { get; set; }
}

public sealed class ImportIssue : ScopedEntity
{
    public Guid ImportBatchId { get; set; }
    public Guid ImportSourceRecordId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ImportIssueSeverity Severity { get; set; }
    public bool Resolved { get; set; }
    public Guid? ResolvedBy { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}

public sealed class ImportCorrection : ScopedEntity
{
    public Guid ImportBatchId { get; set; }
    public Guid ImportSourceRecordId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? OriginalValueJson { get; set; }
    public string? CorrectedValueJson { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class ImportReconciliationCandidate : ScopedEntity
{
    public Guid ImportBatchId { get; set; }
    public Guid SourceRecordId { get; set; }
    public Guid CandidateRecordId { get; set; }
    public string MatchKind { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ImportReconciliationDecision Decision { get; set; }
}

public enum ImportReconciliationDecision { Pending, Linked, Rejected }

public enum ImportSourceType { Annual, Installments, Anticipation, Backup, Catalog, Recurrence, Travel, Scenario }
public enum ImportRecordClass { Record, Header, Section, Subtotal, Total, Model }
public enum ImportIssueSeverity { Error, Warning, Info }
public enum ImportDecision { Pending, Approved, Rejected, Ignored }
