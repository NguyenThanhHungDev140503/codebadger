namespace WebApi.Infrastructure.CentralDb;

using global::Infrastructure.CentralDbSync;
using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.Options;

/// <summary>
/// Registers all CentralDbSync recurring Hangfire jobs.
/// Extracted from Program.cs so it can be tested with the real registrar.
/// </summary>
public sealed class CentralDbSyncRecurringJobRegistrar
{
    private const string Queue = "data-sync";

    private readonly IRecurringJobManager _manager;
    private readonly CentralDbSyncOptions _options;
    private readonly TimeZoneInfo _defaultTz;

    public CentralDbSyncRecurringJobRegistrar(
        IRecurringJobManager manager,
        IOptions<CentralDbSyncOptions> options,
        CentralDbSyncTimeZoneCatalog timeZoneCatalog)
    {
        _manager = manager;
        _options = options.Value;
        _defaultTz = timeZoneCatalog.ResolveUiKey(_options.DefaultTimeZoneKey);
    }

    /// <summary>
    /// Registers all five CentralDbSync recurring jobs:
    /// hot, cold, bootstrap-reconciliation, cleanup, ct-dispatch.
    /// Also removes legacy recurring job IDs.
    /// </summary>
    public void Register()
    {
        // Hot tier
        _manager.AddOrUpdate<CentralDbSyncJobs>(
            "central-db-sync:hot",
            Queue,
            job => job.RunHotAsync(null!, CancellationToken.None),
            _options.HotSchedule,
            new RecurringJobOptions { TimeZone = _defaultTz });

        // Cold tier
        _manager.AddOrUpdate<CentralDbSyncJobs>(
            "central-db-sync:cold",
            Queue,
            job => job.RunColdAsync(null!, CancellationToken.None),
            _options.ColdSchedule,
            new RecurringJobOptions { TimeZone = _defaultTz });

        // Bootstrap stale-scan recurring reconciliation
        _manager.AddOrUpdate<CentralDbSyncJobs>(
            "central-db-sync:reconcile-bootstrap-requests",
            Queue,
            job => job.ReconcilePendingBootstrapRequestsAsync(),
            _options.BootstrapReconciliationCron,
            new RecurringJobOptions { TimeZone = _defaultTz });

        // Orphan stage cleanup — runs daily
        _manager.AddOrUpdate<CentralDbSyncJobs>(
            "central-db-sync:bootstrap-cleanup",
            Queue,
            job => job.RunOrphanStageCleanupAsync(),
            "0 2 * * *",
            new RecurringJobOptions { TimeZone = _defaultTz });

        // CT dispatch reconciliation
        _manager.AddOrUpdate<CentralDbSyncJobs>(
            "central-db-sync:ct-dispatch",
            Queue,
            job => job.RunCtDispatchReconciliationAsync(CancellationToken.None),
            "*/2 * * * *",
            new RecurringJobOptions { TimeZone = _defaultTz });
    }
}
