namespace WebApi.Infrastructure.CentralDb;

/// <summary>Current schedule state for one Central DB sync tier.</summary>
public sealed record CentralDbSyncTierScheduleStateDto(
    string Tier,
    string RecurringJobId,
    string CronExpression,
    string TimeZoneKey,
    string TimeZoneLabel,
    DateTime? NextExecutionUtc,
    string DefaultCronExpression);

/// <summary>Current schedule state returned to the dashboard UI.</summary>
public sealed record CentralDbSyncScheduleStateDto(
    CentralDbSyncTierScheduleStateDto Hot,
    CentralDbSyncTierScheduleStateDto Cold);

/// <summary>Body of an apply-schedule request from the dashboard.</summary>
public sealed record ApplyCentralDbSyncScheduleRequest(
    /// <param name="Tier">Sync tier: <c>Hot</c> or <c>Cold</c>.</param>
    string Tier,
    /// <param name="CronExpression">Five-field cron expression, for example <c>*/5 * * * *</c>.</param>
    string CronExpression,
    /// <param name="TimeZoneKey">Supported UI timezone key, for example <c>vietnam</c>.</param>
    string TimeZoneKey);

/// <summary>Actions the dashboard schedule dispatcher accepts.</summary>
public enum CentralDbSyncScheduleAction
{
    Apply,
    RestoreDefault
}
