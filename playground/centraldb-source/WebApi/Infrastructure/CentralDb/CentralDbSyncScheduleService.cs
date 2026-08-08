using System.Collections.Concurrent;
using Cronos;
using Hangfire;
using Hangfire.Common;
using Infrastructure.CentralDbSync;
using Microsoft.Extensions.Options;

namespace WebApi.Infrastructure.CentralDb;

/// <summary>
/// Reads, validates, applies, and restores Central DB sync recurring-job
/// schedules by SyncTier. All writes go through <see cref="IRecurringJobManager"/>
/// and never enqueue or trigger a job execution.
/// </summary>
public interface ICentralDbSyncScheduleService
{
    /// <summary>Read the current schedule state.</summary>
    Task<CentralDbSyncScheduleStateDto> GetStateAsync();

    /// <summary>Apply a temporary schedule. Does not trigger a sync.</summary>
    Task<CentralDbSyncScheduleStateDto> ApplyAsync(
        ApplyCentralDbSyncScheduleRequest request);

    /// <summary>Restore the configured default schedule for one tier immediately.</summary>
    Task<CentralDbSyncScheduleStateDto> RestoreDefaultAsync(string tier);
}

/// <summary>
/// Reads, validates, applies, and restores Central DB sync recurring-job
/// schedules by SyncTier. All writes go through <see cref="IRecurringJobManager"/>
/// and never enqueue or trigger a job execution.
/// </summary>
public sealed class CentralDbSyncScheduleService : ICentralDbSyncScheduleService
{
    private static readonly ConcurrentDictionary<string, CronExpression> CronCache =
        new(StringComparer.Ordinal);

    private const string HotTier = "Hot";
    private const string ColdTier = "Cold";
    private const string HotRecurringJobId = "central-db-sync:hot";
    private const string ColdRecurringJobId = "central-db-sync:cold";
    private const string Queue = "data-sync";

    private readonly ICentralDbSyncRecurringJobReader _reader;
    private readonly IRecurringJobManager _recurringJobs;
    private readonly CentralDbSyncTimeZoneCatalog _timeZones;
    private readonly IOptions<CentralDbSyncOptions> _options;

    public CentralDbSyncScheduleService(
        ICentralDbSyncRecurringJobReader reader,
        IRecurringJobManager recurringJobs,
        CentralDbSyncTimeZoneCatalog timeZones,
        IOptions<CentralDbSyncOptions> options)
    {
        _reader = reader;
        _recurringJobs = recurringJobs;
        _timeZones = timeZones;
        _options = options;
    }

    /// <summary>Read the current schedule state.</summary>
    public Task<CentralDbSyncScheduleStateDto> GetStateAsync()
    {
        return Task.FromResult(new CentralDbSyncScheduleStateDto(
            BuildState(HotTier),
            BuildState(ColdTier)));
    }

    /// <summary>Apply a temporary schedule. Does not trigger a sync.</summary>
    public Task<CentralDbSyncScheduleStateDto> ApplyAsync(ApplyCentralDbSyncScheduleRequest request)
    {
        return UpdateAsync(request.Tier, request.CronExpression, request.TimeZoneKey);
    }

    /// <summary>Restore the configured default schedule for one tier immediately.</summary>
    public Task<CentralDbSyncScheduleStateDto> RestoreDefaultAsync(string tier)
    {
        var normalized = NormalizeTier(tier);
        return UpdateAsync(normalized, GetDefaultCron(normalized), _options.Value.DefaultTimeZoneKey);
    }

    private Task<CentralDbSyncScheduleStateDto> UpdateAsync(string tier, string cronExpression, string timeZoneKey)
    {
        var normalized = NormalizeTier(tier);
        ValidateFiveFieldCron(cronExpression);
        var timeZone = _timeZones.ResolveUiKey(timeZoneKey);
        var definition = GetDefinition(normalized);

        if (string.Equals(normalized, HotTier, StringComparison.Ordinal))
        {
            _recurringJobs.AddOrUpdate<CentralDbSyncJobs>(
                definition.RecurringJobId,
                Queue,
                job => job.RunHotAsync(null!, CancellationToken.None),
                cronExpression.Trim(),
                new RecurringJobOptions { TimeZone = timeZone });
        }
        else
        {
            _recurringJobs.AddOrUpdate<CentralDbSyncJobs>(
                definition.RecurringJobId,
                Queue,
                job => job.RunColdAsync(null!, CancellationToken.None),
                cronExpression.Trim(),
                new RecurringJobOptions { TimeZone = timeZone });
        }

        return GetStateAsync();
    }

    private CentralDbSyncTierScheduleStateDto BuildState(string tier)
    {
        var definition = GetDefinition(tier);
        var dto = _reader.Get(definition.RecurringJobId);
        if (dto == null)
        {
            var defaultTimeZone = _timeZones.ResolveUiKey(_options.Value.DefaultTimeZoneKey);
            var defaultTimeZoneKey = _timeZones.GetUiKey(defaultTimeZone.Id);
            return new CentralDbSyncTierScheduleStateDto(
                tier,
                definition.RecurringJobId,
                string.Empty,
                defaultTimeZoneKey,
                _timeZones.GetLabel(defaultTimeZoneKey),
                null,
                definition.DefaultCron);
        }

        var timeZoneKey = _timeZones.GetUiKey(dto.TimeZoneId);
        var timeZoneLabel = _timeZones.GetLabel(timeZoneKey);
        var nextExecution = CalculateNextExecution(dto.Cron, dto.TimeZoneId);

        return new CentralDbSyncTierScheduleStateDto(
            tier,
            definition.RecurringJobId,
            dto.Cron,
            timeZoneKey,
            timeZoneLabel,
            nextExecution,
            definition.DefaultCron);
    }

    private static string NormalizeTier(string tier)
    {
        if (string.Equals(tier, HotTier, StringComparison.OrdinalIgnoreCase))
            return HotTier;
        if (string.Equals(tier, ColdTier, StringComparison.OrdinalIgnoreCase))
            return ColdTier;

        throw new ScheduleValidationException("Tier must be either 'Hot' or 'Cold'.");
    }

    private string GetDefaultCron(string tier)
        => string.Equals(tier, HotTier, StringComparison.Ordinal)
            ? _options.Value.HotSchedule
            : _options.Value.ColdSchedule;

    private TierScheduleDefinition GetDefinition(string tier)
        => string.Equals(tier, HotTier, StringComparison.Ordinal)
            ? new TierScheduleDefinition(HotRecurringJobId, _options.Value.HotSchedule)
            : new TierScheduleDefinition(ColdRecurringJobId, _options.Value.ColdSchedule);

    private static void ValidateFiveFieldCron(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
            throw new ScheduleValidationException("CRON expression is required.");

        if (!CronExpression.TryParse(cronExpression.Trim(), CronFormat.Standard, out _))
            throw new ScheduleValidationException(
                $"Invalid five-field CRON expression: '{cronExpression.Trim()}'.");
    }

    private static DateTime? CalculateNextExecution(string cronExpression, string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
            return null;

        if (!TryNormalizeCron(cronExpression, out var normalized))
            return null;

        var cron = CronCache.GetOrAdd(normalized, static expr =>
            CronExpression.Parse(expr, CronFormat.Standard));

        var tz = ResolveTimeZone(timeZoneId);
        var now = DateTime.UtcNow;
        var next = cron.GetNextOccurrence(now, tz, inclusive: false);
        return next?.Kind == DateTimeKind.Utc ? next : next?.ToUniversalTime();
    }

    private static bool TryNormalizeCron(string cronExpression, out string normalized)
    {
        normalized = string.Empty;

        var parts = cronExpression
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 6)
        {
            // Convert "sec min hour day month dow" => "min hour day month dow"
            normalized = string.Join(' ', parts.Skip(1));
            return true;
        }

        if (parts.Length == 5)
        {
            normalized = string.Join(' ', parts);
            return true;
        }

        return false;
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return TimeZoneInfo.Utc;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            var mapped = timeZoneId switch
            {
                "Asia/Ho_Chi_Minh" => "SE Asia Standard Time",
                "Etc/UTC" => "UTC",
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(mapped))
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(mapped); }
                catch { }
            }

            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    private sealed record TierScheduleDefinition(
        string RecurringJobId,
        string DefaultCron);
}

/// <summary>
/// Exception indicating a validation failure in schedule operations.
/// Caught by the dispatcher and returned as a 422 response.
/// </summary>
public sealed class ScheduleValidationException : Exception
{
    public ScheduleValidationException(string message) : base(message) { }
}
