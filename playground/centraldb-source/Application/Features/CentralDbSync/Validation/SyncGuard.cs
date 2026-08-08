using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Models;

namespace Application.Features.CentralDbSync.Validation;

/// <summary>
/// Guards that validate Central DB Sync enum values before they hit the database.
/// Every CHECK constraint in the schema should have a corresponding guard here.
/// </summary>
public static class SyncGuard
{
    private static readonly HashSet<string> ValidSyncModes =
        ["FullRefresh", "ChangeTracking"];

    private static readonly HashSet<string> ValidSyncTiers =
        ["Hot", "Cold"];

    private static readonly HashSet<string> ValidCheckpointStatuses =
    [
        SyncStatus.CheckpointState.PendingInitialSync,
        SyncStatus.CheckpointState.Ready,
        SyncStatus.CheckpointState.RequiresFullResync
    ];

    private static readonly HashSet<string> ValidOutcomes =
    [
        SyncStatus.Outcome.Succeeded,
        SyncStatus.Outcome.NoChanges,
        SyncStatus.Outcome.Failed,
        SyncStatus.Outcome.SkippedLocked,
        SyncStatus.Outcome.SkippedDependency,
        SyncStatus.Outcome.RequiresFullResync
    ];

    private static readonly HashSet<string> ValidBootstrapStatuses =
    [
        BootstrapRequestStatus.PendingEnqueue,
        BootstrapRequestStatus.Queued,
        BootstrapRequestStatus.Running,
        BootstrapRequestStatus.WaitingForLock,
        BootstrapRequestStatus.Completed,
        BootstrapRequestStatus.Failed
    ];

    public static void AssertValidSyncMode(string value, string paramName)
    {
        if (!ValidSyncModes.Contains(value))
            throw new ArgumentException(
                $"'{value}' is not a valid sync mode. Allowed: {string.Join(", ", ValidSyncModes)}",
                paramName);
    }

    public static void AssertValidSyncTier(string value, string paramName)
    {
        if (!ValidSyncTiers.Contains(value))
            throw new ArgumentException(
                $"'{value}' is not a valid sync tier. Allowed: {string.Join(", ", ValidSyncTiers)}",
                paramName);
    }

    public static void AssertValidCheckpointStatus(string value, string paramName)
    {
        if (!ValidCheckpointStatuses.Contains(value))
            throw new ArgumentException(
                $"'{value}' is not a valid checkpoint status. Allowed: {string.Join(", ", ValidCheckpointStatuses)}",
                paramName);
    }

    public static void AssertValidOutcome(string value, string paramName)
    {
        if (!ValidOutcomes.Contains(value))
            throw new ArgumentException(
                $"'{value}' is not a valid sync run outcome. Allowed: {string.Join(", ", ValidOutcomes)}",
                paramName);
    }

    public static void AssertValidBootstrapStatus(string value, string paramName)
    {
        if (!ValidBootstrapStatuses.Contains(value))
            throw new ArgumentException(
                $"'{value}' is not a valid bootstrap request status. Allowed: {string.Join(", ", ValidBootstrapStatuses)}",
                paramName);
    }

    public static void AssertValidConfig(TableSyncConfig config)
    {
        AssertNotEmpty(config.SourceTable, nameof(config.SourceTable));
        AssertValidSyncMode(config.SyncMode, nameof(config.SyncMode));
        AssertValidSyncTier(config.SyncTier, nameof(config.SyncTier));
    }

    public static void AssertNotEmpty(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"'{paramName}' cannot be null or empty.", paramName);
    }

    /// <summary>
    /// Validates that <paramref name="ruleName"/> is a registered mapping rule.
    /// This is the application-layer referential guard that replaces DB-level FK constraints.
    /// </summary>
    public static void AssertRegisteredRule(
        string ruleName, IMappingRuleProvider ruleProvider, string paramName)
    {
        if (!ruleProvider.TryGet(ruleName, out _))
        {
            var allowed = string.Join(", ", ruleProvider.GetAll().Select(r => r.RuleName));
            throw new ArgumentException(
                $"'{ruleName}' is not a registered sync rule. Allowed: {allowed}",
                paramName);
        }
    }
}
