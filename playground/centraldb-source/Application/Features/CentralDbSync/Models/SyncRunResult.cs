namespace Application.Features.CentralDbSync.Models;

public sealed record SyncRunResult
{
    public string Outcome { get; init; } = SyncStatus.Outcome.Succeeded;
    public int RowsRead { get; init; }
    public int RowsUpserted { get; init; }
    public int RowsDeactivated { get; init; }
    public int RowsDeleted { get; init; }
    public long? CheckpointBefore { get; init; }
    public long? CheckpointAfter { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    /// <summary>
    /// JSON snapshot of per-record details: operation counts, sample primary keys,
    /// and (if applicable) the record that caused a failure.
    /// </summary>
    public string? RowDetailsJson { get; init; }
}
