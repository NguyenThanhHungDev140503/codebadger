namespace Infrastructure.CentralDbSync;

using Application.Features.CentralDbSync.Abstractions;
using Dapper;
using Npgsql;

public sealed class PostgresTableSyncLock(
    string connectionString)
    : ITableSyncLock
{
    public async Task<IAsyncDisposable?> TryAcquireAsync(
        string sourceTable,
        CancellationToken ct,
        TimeSpan leaseTimeout = default)
    {
        var lockKey = GetStableLockHash($"central-db-sync:{sourceTable}");

        var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        try
        {
            var acquired = await conn.ExecuteScalarAsync<bool>(
                "SELECT pg_try_advisory_lock(@lockKey)",
                new { lockKey });

            if (!acquired)
            {
                await conn.DisposeAsync();
                return null;
            }

            var lease = leaseTimeout > TimeSpan.Zero
                ? leaseTimeout
                : TimeSpan.FromMinutes(10);
            return new AdvisoryLockHandle(conn, lockKey, lease);
        }
        catch
        {
            await conn.DisposeAsync();
            throw;
        }
    }

    private static long GetStableLockHash(string key)
    {
        // FNV-1a 64-bit hash — deterministic across runs
        unchecked
        {
            ulong hash = 14695981039346656037;
            foreach (var c in key)
            {
                hash ^= c;
                hash *= 1099511628211;
            }
            return (long)hash;
        }
    }

    private sealed class AdvisoryLockHandle : IAsyncDisposable
    {
        private readonly NpgsqlConnection _connection;
        private readonly long _lockKey;
        private readonly CancellationTokenSource _watchdogCts;
        private int _disposed;

        /// <summary>
        /// Creates an advisory lock handle with a watchdog that force-releases
        /// the lock after <paramref name="leaseTimeout"/> if DisposeAsync is not
        /// called (e.g. app process hangs).
        /// </summary>
        public AdvisoryLockHandle(NpgsqlConnection connection, long lockKey, TimeSpan leaseTimeout)
        {
            _connection = connection;
            _lockKey = lockKey;
            _watchdogCts = new CancellationTokenSource();
            _ = WatchdogAsync(leaseTimeout, _watchdogCts.Token);
        }

        /// <summary>
        /// Watchdog task: waits for the lease timeout, then force-releases the lock.
        /// Cancelled by DisposeAsync on the happy path.
        /// </summary>
        private async Task WatchdogAsync(TimeSpan leaseTimeout, CancellationToken ct)
        {
            try
            {
                await Task.Delay(leaseTimeout, ct);
                // Timeout elapsed — caller never disposed us (hung) → force release
                await ForceReleaseAsync();
            }
            catch (OperationCanceledException)
            {
                // Happy-path dispose cancelled the watchdog — nothing to do
            }
        }

        /// <summary>
        /// Happy-path release. Cancels the watchdog so it does not also try to clean up.
        /// Thread-safe via Interlocked guard — only one thread (this or ForceReleaseAsync)
        /// executes the unlock + connection dispose.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                return;

            // Cancel watchdog so it doesn't also try to force-release
            try { _watchdogCts.Cancel(); } catch (ObjectDisposedException) { }
            try { _watchdogCts.Dispose(); } catch (ObjectDisposedException) { }

            try
            {
                await _connection.ExecuteAsync(
                    "SELECT pg_advisory_unlock(@lockKey)",
                    new { lockKey = _lockKey });
            }
            finally
            {
                await _connection.DisposeAsync();
            }
        }

        /// <summary>
        /// Force-release path called by the watchdog when lease timeout elapses.
        /// Only one thread (this or DisposeAsync) passes the Interlocked guard.
        /// </summary>
        private async Task ForceReleaseAsync()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                return;

            try { _watchdogCts.Dispose(); } catch (ObjectDisposedException) { }

            try
            {
                await _connection.ExecuteAsync(
                    "SELECT pg_advisory_unlock(@lockKey)",
                    new { lockKey = _lockKey });
            }
            catch
            {
                // Best-effort unlock — connection dispose cleans up session-level lock
            }
            finally
            {
                await _connection.DisposeAsync();
            }
        }
    }
}
