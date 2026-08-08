using Application.Features.CentralDbSync.Models;

namespace Application.Features.CentralDbSync.Abstractions;

public interface ISyncBatchApplier
{
    Task<SyncRunResult> ApplyBatchAsync(
        TableSyncConfig config,
        ChangeBatch batch,
        CancellationToken cancellationToken);

    Task<SyncRunResult> ApplyBootstrapAsync(
        TableSyncConfig config,
        BootstrapSnapshot snapshot,
        CancellationToken cancellationToken);
}
