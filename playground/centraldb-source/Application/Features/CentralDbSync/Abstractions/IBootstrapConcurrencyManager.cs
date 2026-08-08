namespace Application.Features.CentralDbSync.Abstractions;

/// <summary>
/// Performance governor for scalable bootstrap operations.
/// Limits concurrent child job bodies and DDL operations (CREATE/DROP staging tables)
/// via SemaphoreSlim. This is NOT a correctness boundary — CAS/fencing and
/// transactions provide correctness. The governor only prevents overload of
/// SQL Server and PostgreSQL when many parents run simultaneously.
/// </summary>
/// <remarks>
/// Currently uses in-process SemaphoreSlim, which assumes a single Hangfire
/// server instance. If scaled to multiple instances, replace with a distributed
/// semaphore (e.g. Redis-based or Hangfire-backend-based).
/// </remarks>
public interface IBootstrapConcurrencyManager
{
    /// <summary>
    /// Non-blocking attempt to acquire the child concurrency slot.
    /// Returns false if the limit (BootstrapChildConcurrency) is reached.
    /// </summary>
    bool TryAcquireChild();

    /// <summary>
    /// Releases a previously acquired child concurrency slot.
    /// </summary>
    void ReleaseChild();

    /// <summary>
    /// Non-blocking attempt to acquire the stage DDL concurrency slot.
    /// Returns false if the limit (BootstrapStageDdlConcurrency) is reached.
    /// </summary>
    bool TryAcquireStageDdl();

    /// <summary>
    /// Releases a previously acquired stage DDL concurrency slot.
    /// </summary>
    void ReleaseStageDdl();
}
