using Application.Features.CentralDbSync.Models;

namespace Application.Features.CentralDbSync.Abstractions;

public interface ISyncRunLog
{
    Task WriteAsync(
        SyncRunLogEntry entry,
        CancellationToken cancellationToken);
}
