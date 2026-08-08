using Application.Features.CentralDbSync.Models;

namespace Application.Features.CentralDbSync.Abstractions;

public interface IBootstrapSnapshotReader
{
    Task<BootstrapSnapshot> ReadAsync(
        TableSyncConfig config,
        CancellationToken cancellationToken);
}
