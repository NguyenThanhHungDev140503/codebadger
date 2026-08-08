namespace Application.Features.CentralDbSync.Models;

/// <summary>
/// Status values for <see cref="BootstrapChild"/> lifecycle.
/// Active states: PendingEnqueue, Queued, Running.
/// Terminal states: Completed, Failed.
/// </summary>
public static class BootstrapChildStatus
{
    public const string PendingEnqueue = "pending_enqueue";
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Completed = "completed";
    public const string Failed = "failed";
}
