namespace Application.Features.CentralDbSync.Dtos;

public sealed record TableSyncOverviewDto(
    string RuleName,
    bool IsEnabled,
    string SyncTier,
    string SyncStatus,
    string HealthStatus,
    long? LastSyncVersion,
    DateTime? LastAttemptAt,
    DateTime? LastSuccessAt,
    DateTime? LastFailureAt,
    TimeSpan ExpectedSyncInterval,
    TimeSpan MaxAllowedLag,
    long? LastSyncLagMs,
    int ConsecutiveFailureCount,
    string? LastErrorCode,
    string? LastErrorMessage,
    string? LatestRunOutcome,
    DateTime? LatestRunStartedAt,
    long? LatestRunDurationMs,
    int? LatestRunRowsUpserted);
