using Application.Features.CentralDbSync.Models;

namespace Application.Features.CentralDbSync.Abstractions;

public interface ISyncCheckpointStore
{
    Task<SyncCheckpointState?> GetAsync(
        string sourceTable,
        CancellationToken cancellationToken);

    /// <summary>
    /// Atomically advances the checkpoint with an optimistic concurrency guard.
    /// Returns true if the checkpoint was advanced; false if another worker already moved it.
    /// </summary>
    Task<bool> AdvanceAsync(
        string sourceTable,
        long previousCheckpoint,
        long nextCheckpoint,
        CancellationToken cancellationToken = default);

    Task TransitionToFullResyncAsync(
        string sourceTable,
        string? errorCode = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);
}
