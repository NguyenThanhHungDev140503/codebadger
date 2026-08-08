namespace Application.Features.CentralDbSync.Models;

/// <summary>
/// Status values for <see cref="BootstrapParent"/> lifecycle.
/// Active states: PendingEnqueue, Running, CatchingUp, Publishing, RecoveryPending, CancelRequested.
/// Terminal states: Completed, Failed, Expired, Cancelled.
/// </summary>
public static class BootstrapParentStatus
{
    public const string PendingEnqueue = "pending_enqueue";
    public const string Running = "running";
    public const string CatchingUp = "catching_up";
    public const string Publishing = "publishing";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string RecoveryPending = "recovery_pending";
    public const string Expired = "expired";
    public const string CancelRequested = "cancel_requested";
    public const string Cancelled = "cancelled";
}
