using Application.Features.CentralDbSync.Models;

namespace Application.Features.CentralDbSync.Services;

/// <summary>
/// Encodes the next recovery action a reconciler or claimed-job
/// dispatcher should take for a given parent status.
/// </summary>
public enum BootstrapParentRecoveryAction
{
    /// <summary>Schedule a fenced parent-start job.</summary>
    StartPending,
    /// <summary>Resume the child chain (Running phase).</summary>
    ResumeRunning,
    /// <summary>Resume the CT catch-up phase.</summary>
    ResumeCatchingUp,
    /// <summary>Resume the publish phase.</summary>
    ResumePublishing,
    /// <summary>Already recovery-pending — no-op.</summary>
    RecoveryPending,
    /// <summary>Mark request Completed.</summary>
    SyncCompleted,
    /// <summary>Propagate parent failure to request.</summary>
    SyncFailed,
    /// <summary>Unsupported/unexpected status.</summary>
    Unknown
}

/// <summary>
/// Shared policy for classifying a parent's recovery action.
/// Used by reconciliation, claimed-job dispatch, and coordinator resume.
/// </summary>
public static class BootstrapParentRecoveryClassifier
{
    public static BootstrapParentRecoveryAction Classify(BootstrapParent parent)
    {
        return parent.Status switch
        {
            BootstrapParentStatus.PendingEnqueue => BootstrapParentRecoveryAction.StartPending,
            BootstrapParentStatus.Running => BootstrapParentRecoveryAction.ResumeRunning,
            BootstrapParentStatus.CatchingUp => BootstrapParentRecoveryAction.ResumeCatchingUp,
            BootstrapParentStatus.Publishing => BootstrapParentRecoveryAction.ResumePublishing,
            BootstrapParentStatus.RecoveryPending => BootstrapParentRecoveryAction.RecoveryPending,
            BootstrapParentStatus.Completed => BootstrapParentRecoveryAction.SyncCompleted,
            BootstrapParentStatus.Failed or BootstrapParentStatus.Expired
                or BootstrapParentStatus.CancelRequested or BootstrapParentStatus.Cancelled
                => BootstrapParentRecoveryAction.SyncFailed,
            _ => BootstrapParentRecoveryAction.Unknown
        };
    }
}
