using Application.Features.CentralDbSync.Models;

namespace Application.Features.CentralDbSync.Abstractions;

public interface IChangeTrackingReader
{
    Task<ChangeBatch> ReadBatchAsync(
        TableSyncConfig config,
        long checkpoint,
        CancellationToken cancellationToken);
}
