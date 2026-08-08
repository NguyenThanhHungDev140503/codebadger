using Application.Common.Models;
using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Dtos;
using Application.Features.CentralDbSync.Queries;
using MediatR;

namespace Application.Features.CentralDbSync.Handlers;

public sealed class GetSyncOverviewQueryHandler(ICentralDbSyncQueryService queryService)
    : IRequestHandler<GetSyncOverviewQuery, ApiResponse<SyncOverviewDto>>
{
    public async Task<ApiResponse<SyncOverviewDto>> Handle(
        GetSyncOverviewQuery request, CancellationToken ct)
    {
        var result = await queryService.GetOverviewAsync(ct);
        return ApiResponse<SyncOverviewDto>.Success(result);
    }
}
