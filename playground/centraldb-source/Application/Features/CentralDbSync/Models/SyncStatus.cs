namespace Application.Features.CentralDbSync.Models;

public static class SyncStatus
{
    public static class Outcome
    {
        public const string Succeeded = "succeeded";
        public const string NoChanges = "no_changes";
        public const string Failed = "failed";
        public const string SkippedLocked = "skipped_locked";
        public const string SkippedDependency = "skipped_dependency";
        public const string RequiresFullResync = "requires_full_resync";
    }

    public static class CheckpointState
    {
        public const string PendingInitialSync = "pending_initial_sync";
        public const string Ready = "ready";
        public const string RequiresFullResync = "requires_full_resync";
    }
}
