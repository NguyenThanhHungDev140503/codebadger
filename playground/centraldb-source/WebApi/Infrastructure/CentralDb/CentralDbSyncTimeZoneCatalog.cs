namespace WebApi.Infrastructure.CentralDb;

/// <summary>
/// Allow-listed timezone keys that map to stable UI labels and accept
/// either Windows or IANA stored IDs.
/// </summary>
public sealed class CentralDbSyncTimeZoneCatalog
{
    private readonly Func<string, TimeZoneInfo> _findById;

    public const string UtcKey = "utc";
    public const string VietnamKey = "vietnam";

    public CentralDbSyncTimeZoneCatalog(Func<string, TimeZoneInfo>? findById = null)
    {
        _findById = findById ?? TimeZoneInfo.FindSystemTimeZoneById;
    }

    /// <summary>Resolve a UI key to the host-specific TimeZoneInfo.</summary>
    public TimeZoneInfo ResolveUiKey(string key) => key switch
    {
        UtcKey => TimeZoneInfo.Utc,
        VietnamKey => ResolveFirst(["SE Asia Standard Time", "Asia/Ho_Chi_Minh"]),
        _ => throw new TimeZoneNotFoundException($"Unsupported timezone key '{key}'.")
    };

    /// <summary>Friendly label for a UI key.</summary>
    public string GetLabel(string key) => key switch
    {
        UtcKey => "UTC",
        VietnamKey => "Vietnam (UTC+07:00)",
        _ => throw new TimeZoneNotFoundException($"Unsupported timezone key '{key}'.")
    };

    /// <summary>Map a stored timezone ID back to a UI key.</summary>
    public string GetUiKey(string timeZoneId) => timeZoneId switch
    {
        "UTC" => UtcKey,
        "SE Asia Standard Time" or "Asia/Ho_Chi_Minh" => VietnamKey,
        _ => throw new TimeZoneNotFoundException($"Unsupported timezone id '{timeZoneId}'.")
    };

    /// <summary>Supported UI keys for enumeration.</summary>
    public IReadOnlyList<string> SupportedKeys => [UtcKey, VietnamKey];

    private TimeZoneInfo ResolveFirst(string[] aliases)
    {
        TimeZoneNotFoundException? last = null;
        foreach (var alias in aliases)
        {
            try { return _findById(alias); }
            catch (TimeZoneNotFoundException ex) { last = ex; }
            catch (InvalidTimeZoneException ex) { last = new TimeZoneNotFoundException(alias, ex); }
        }
        throw last ?? new TimeZoneNotFoundException("No timezone alias could be resolved.");
    }
}
