using System.Text.Json.Serialization;

namespace Application.Common.Models;

/// <summary>
/// Payload chuẩn cho POST/PUT — FE nhận object thay vì scalar.
/// Version tạm không trả (null → omit JSON); bật lại sau bằng From(id, version).
/// Id dùng long để cover cả entity int và ReportDateFilterOption (long).
/// </summary>
public class CommandResponse
{
    public long Id { get; init; }

    /// <summary>
    /// Optimistic locking — tạm thời không trả về FE.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Version { get; init; }

    public static CommandResponse From(long id, int? version = null)
        => new() { Id = id, Version = version };
}