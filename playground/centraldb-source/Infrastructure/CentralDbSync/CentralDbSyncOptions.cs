namespace Infrastructure.CentralDbSync;

public sealed class CentralDbSyncOptions
{
    public const string SectionName = "CentralDbSync";

    /// <summary>Connection string for the target PostgreSQL Central DB.</summary>
    public required string CentralDbConnection { get; set; }

    /// <summary>Rule name for the pilot.</summary>
    public string PilotRuleName { get; set; } = "CRM.Partners";

    /// <summary>Cron schedule expression for the pilot hot job.</summary>
    public string PilotSchedule { get; set; } = "*";  // Cron.Minutely()

    /// <summary>
    /// Legacy cron schedule for the all-rules central-db-sync job. Kept for compatibility;
    /// new recurring registrations use HotSchedule and ColdSchedule.
    /// </summary>
    public string SyncSchedule { get; set; } = "*/5 * * * *";

    /// <summary>Cron schedule expression for Hot central DB sync rules.</summary>
    public string HotSchedule { get; set; } = "*/5 * * * *";

    /// <summary>Cron schedule expression for Cold central DB sync rules.</summary>
    public string ColdSchedule { get; set; } = "0 */1 * * *";

    /// <summary>Default UI timezone key ("utc" or "vietnam") for new and restored schedules.</summary>
    public string DefaultTimeZoneKey { get; set; } = "vietnam";

    /// <summary>Maximum allowed lag before health becomes Degraded.</summary>
    public TimeSpan MaxAllowedLag { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Hangfire queue name for child bootstrap jobs.</summary>
    public string BootstrapChildQueue { get; set; } = "bootstrap-child";

    /// <summary>Maximum number of concurrent child bootstrap jobs system-wide.</summary>
    public int BootstrapChildConcurrency { get; set; } = 2;

    /// <summary>Maximum number of concurrent CREATE/DROP stage DDL operations.</summary>
    public int BootstrapStageDdlConcurrency { get; set; } = 2;

    /// <summary>Child batch size (max rows per child job).</summary>
    public int BootstrapChildBatchSize { get; set; } = 10_000;

    /// <summary>Child transient retry count.</summary>
    public int BootstrapChildRetryCount { get; set; } = 3;

    /// <summary>Child execution timeout.</summary>
    public TimeSpan BootstrapChildTimeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Final publish timeout.</summary>
    public TimeSpan BootstrapFinalPublishTimeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Orphan stage retention before cleanup.</summary>
    public TimeSpan BootstrapOrphanStageRetention { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Time after which a bootstrap request in 'running' status is considered stale for inspection.
    /// </summary>
    public TimeSpan BootstrapRunningStaleAfter { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Time after which a bootstrap request in 'waiting_for_lock' status is considered stale for inspection.
    /// </summary>
    public TimeSpan BootstrapWaitingForLockStaleAfter { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Stale threshold for pending_enqueue and queued requests in batch reconciliation.
    /// Requests updated more recently are considered too fresh for the batch watchdog.
    /// </summary>
    public TimeSpan BootstrapIdleReconciliationAfter { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Cron schedule expression for the recurring bootstrap reconciliation job.</summary>
    public string BootstrapReconciliationCron { get; set; } = "*/2 * * * *";
}
