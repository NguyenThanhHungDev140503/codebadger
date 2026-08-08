namespace Application.Features.CentralDbSync.Models;

/// <summary>
/// Distinguishes one-shot watchdog recovery from batch stale-scan reconciliation.
/// </summary>
public enum BootstrapReconciliationMode
{
    /// <summary>One-shot watchdog scheduled after request submission. Never recovers active requests.</summary>
    OneShot,

    /// <summary>Batch stale-scan from recurring or manual reconciliation. May probe stale active requests.</summary>
    StaleScan
}

/// <summary>
/// Immutable context passed to reconciliation methods. Determines which request
/// statuses are eligible for inspection and recovery.
/// </summary>
public sealed record BootstrapReconciliationContext(
    BootstrapReconciliationMode Mode,
    DateTime ObservedAtUtc);
