namespace Infrastructure.CentralDbSync;

using Application.Features.CentralDbSync.Abstractions;
using Microsoft.Extensions.Options;

/// <summary>
/// In-process SemaphoreSlim-based concurrency governor for scalable bootstrap.
/// Limits concurrent child job bodies and DDL operations.
///
/// NOTE: This assumes a single Hangfire server instance. If the deployment is
/// scaled to multiple instances, replace with a distributed semaphore
/// (e.g. Redis-based or using Hangfire storage).
/// </summary>
public sealed class BootstrapConcurrencyManager : IBootstrapConcurrencyManager, IDisposable
{
    private readonly SemaphoreSlim _childSemaphore;
    private readonly SemaphoreSlim _stageDdlSemaphore;

    public BootstrapConcurrencyManager(IOptions<CentralDbSyncOptions> options)
    {
        var opts = options.Value;
        _childSemaphore = new SemaphoreSlim(
            opts.BootstrapChildConcurrency,
            opts.BootstrapChildConcurrency);
        _stageDdlSemaphore = new SemaphoreSlim(
            opts.BootstrapStageDdlConcurrency,
            opts.BootstrapStageDdlConcurrency);
    }

    public bool TryAcquireChild()
        => _childSemaphore.Wait(0);

    public void ReleaseChild()
    {
        try { _childSemaphore.Release(); }
        catch (SemaphoreFullException) { }
    }

    public bool TryAcquireStageDdl()
        => _stageDdlSemaphore.Wait(0);

    public void ReleaseStageDdl()
    {
        try { _stageDdlSemaphore.Release(); }
        catch (SemaphoreFullException) { }
    }

    public void Dispose()
    {
        _childSemaphore.Dispose();
        _stageDdlSemaphore.Dispose();
    }
}
