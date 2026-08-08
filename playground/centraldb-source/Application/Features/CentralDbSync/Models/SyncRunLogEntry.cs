namespace Application.Features.CentralDbSync.Models;

public sealed record SyncRunLogEntry
{
    public Guid RunId { get; init; } = Guid.NewGuid();
    public required string SourceTable { get; init; }
    public required string Mode { get; init; }
    public required string Outcome { get; init; }
    public int RowsRead { get; init; }
    public int RowsUpserted { get; init; }
    public int RowsDeactivated { get; init; }
    public int RowsDeleted { get; init; }
    public long? CheckpointBefore { get; init; }
    public long? CheckpointAfter { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? FinishedAt { get; init; }
    public int? DurationMs { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    /// <summary>
    /// JSON snapshot of per-record details (operation counts, sample primary keys, etc.).
    /// Carried through from <see cref="SyncRunResult.RowDetailsJson"/>.
    /// </summary>
    public string? RowDetailsJson { get; init; }
}
