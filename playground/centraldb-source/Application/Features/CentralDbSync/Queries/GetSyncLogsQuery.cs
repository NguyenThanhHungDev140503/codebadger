using Application.Common.Models;
using Application.Features.CentralDbSync.Dtos;

namespace Application.Features.CentralDbSync.Queries;

public sealed class GetSyncLogsQuery
    : PaginationRequest<PaginatedResponse<SyncRunLogDto>>
{
    /// <summary>Optional exact rule-name filter.</summary>
    public string? RuleName { get; set; }
    /// <summary>Optional sync outcome filter, such as succeeded, failed or skipped.</summary>
    public string? Outcome { get; set; }
    /// <summary>Optional inclusive start date/time. Use ISO-8601; date-only values may use <c>yyyy-MM-dd</c>.</summary>
    public DateTime? From { get; set; }
    /// <summary>Optional inclusive end date/time. Use ISO-8601; date-only values may use <c>yyyy-MM-dd</c>.</summary>
    public DateTime? To { get; set; }
}
