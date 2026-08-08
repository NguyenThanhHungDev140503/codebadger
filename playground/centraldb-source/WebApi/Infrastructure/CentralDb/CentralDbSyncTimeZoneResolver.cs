using Hangfire;

namespace WebApi.Infrastructure.CentralDb;

/// <summary>
/// Hangfire timezone resolver that accepts both Windows and IANA IDs
/// for the supported timezone keys, so persisted schedules are portable
/// between Windows deployment hosts and Linux developer hosts.
/// </summary>
public sealed class CentralDbSyncTimeZoneResolver : ITimeZoneResolver
{
    private readonly CentralDbSyncTimeZoneCatalog _catalog;

    public CentralDbSyncTimeZoneResolver(CentralDbSyncTimeZoneCatalog catalog)
    {
        _catalog = catalog;
    }

    public TimeZoneInfo GetTimeZoneById(string timeZoneId)
    {
        // UTC is always UTC regardless of host.
        if (string.Equals(timeZoneId, "UTC", StringComparison.OrdinalIgnoreCase))
            return TimeZoneInfo.Utc;

        // Accept either Windows or IANA alias for Vietnam.
        if (string.Equals(timeZoneId, "SE Asia Standard Time", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(timeZoneId, "Asia/Ho_Chi_Minh", StringComparison.OrdinalIgnoreCase))
        {
            return _catalog.ResolveUiKey(CentralDbSyncTimeZoneCatalog.VietnamKey);
        }

        // Fall back to host system lookup for any other timezone
        // so existing Hangfire jobs with unrelated timezones remain resolvable.
        return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    }
}
