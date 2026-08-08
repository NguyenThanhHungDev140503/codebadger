namespace Application.Features.CentralDbSync.Models;

public sealed record SyncCheckpointState
{
    public required string SourceTable { get; init; }
    public long? LastSyncVersion { get; init; }
    public string SyncStatus { get; init; } = Models.SyncStatus.CheckpointState.PendingInitialSync;
    public DateTime? LastAttemptAt { get; init; }
    public DateTime? LastSuccessAt { get; init; }
    public DateTime? LastFailureAt { get; init; }
    public int ConsecutiveFailureCount { get; init; }
    public string? LastErrorCode { get; init; }
    public string? LastErrorMessage { get; init; }
}
