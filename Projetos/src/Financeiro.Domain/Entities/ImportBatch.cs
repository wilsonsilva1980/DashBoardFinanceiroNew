namespace Financeiro.Domain.Entities;

public sealed class ImportBatch : ScopedEntity
{
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string ParserVersion { get; set; } = string.Empty;
    public ImportState State { get; set; } = ImportState.Received;
    public string StatisticsJson { get; set; } = "{}";
    public Guid? ConfirmedBy { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public string? FailureReason { get; set; }
}

public enum ImportState { Received, Analyzed, AwaitingReview, Confirmed, Failed, Cancelled }
