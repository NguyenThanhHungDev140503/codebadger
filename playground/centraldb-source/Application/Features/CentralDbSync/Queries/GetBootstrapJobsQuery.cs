using Application.Common.Models;
using Application.Features.CentralDbSync.Dtos;

namespace Application.Features.CentralDbSync.Queries;

public sealed class GetBootstrapJobsQuery
    : PaginationRequest<PaginatedResponse<BootstrapJobListItemDto>>
{
    /// <summary>Optional exact rule-name filter, for example <c>ERP.PurchaseOrders</c>.</summary>
    public string? RuleName { get; set; }
    /// <summary>Optional bootstrap status filter: pending_enqueue, queued, running, waiting_for_lock, completed or failed.</summary>
    public string? Status { get; set; }
}
