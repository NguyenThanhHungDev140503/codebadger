using Application.Common.Models;
using Application.Features.CentralDbSync.Dtos;
using MediatR;

namespace Application.Features.CentralDbSync.Queries;

public sealed class GetBootstrapMonitorListQuery
    : PaginationRequest<PaginatedResponse<BootstrapMonitorListItemDto>>
{
    public string? RuleName { get; set; }
    public string? Status { get; set; }
}
