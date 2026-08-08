namespace Application.Features.CentralDbSync.Abstractions;

/// <summary>
/// Provider-neutral reconciliation policy used by Application-layer services.
/// </summary>
public interface IBootstrapReconciliationPolicy
{
    /// <summary>
    /// Minimum age before a <c>pending_enqueue</c> or <c>queued</c> request is
    /// eligible for batch reconciliation inspection.
    /// </summary>
    TimeSpan IdleAfter { get; }

    /// <summary>
    /// Minimum age before a <c>running</c> request is eligible for stale inspection.
    /// </summary>
    TimeSpan RunningStaleAfter { get; }

    /// <summary>
    /// Minimum age before a <c>waiting_for_lock</c> request is eligible for stale inspection.
    /// </summary>
    TimeSpan WaitingForLockStaleAfter { get; }

    /// <summary>
    /// Maximum number of automatic recovery attempts before the request is marked
    /// permanently failed. Must match the SQL CAS guard.
    /// </summary>
    int MaxRecoveryAttempts { get; }
}
