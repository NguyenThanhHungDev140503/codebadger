using Application.Common.Models;
using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Dtos;
using Application.Features.CentralDbSync.Queries;
using MediatR;

namespace Application.Features.CentralDbSync.Handlers;

public sealed class GetBootstrapMonitorListQueryHandler(IBootstrapMonitorQueryService queryService)
    : IRequestHandler<GetBootstrapMonitorListQuery, ApiResponse<PaginatedResponse<BootstrapMonitorListItemDto>>>
{
    public async Task<ApiResponse<PaginatedResponse<BootstrapMonitorListItemDto>>> Handle(
        GetBootstrapMonitorListQuery request, CancellationToken ct)
    {
        var items = await queryService.GetRequestListAsync(
            request.RuleName, request.Status, request.PageIndex, request.PageSize, ct);

        var result = PaginatedResponse<BootstrapMonitorListItemDto>.Create(
            items, request.PageIndex, request.PageSize);

        return ApiResponse<PaginatedResponse<BootstrapMonitorListItemDto>>.Success(result);
    }
}
