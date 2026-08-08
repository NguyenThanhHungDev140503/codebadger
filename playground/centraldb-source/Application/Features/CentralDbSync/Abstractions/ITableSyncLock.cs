namespace Application.Features.CentralDbSync.Abstractions;

public interface ITableSyncLock
{
    Task<IAsyncDisposable?> TryAcquireAsync(
        string sourceTable,
        CancellationToken cancellationToken,
        TimeSpan leaseTimeout = default);
}
