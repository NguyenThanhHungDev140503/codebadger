using Application.Common.Models;
using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Dtos;
using Application.Features.CentralDbSync.Queries;
using MediatR;

namespace Application.Features.CentralDbSync.Handlers;

public sealed class GetMonitoringStatsQueryHandler(ICentralDbSyncQueryService queryService)
    : IRequestHandler<GetMonitoringStatsQuery, ApiResponse<MonitoringStatsDto>>
{
    public async Task<ApiResponse<MonitoringStatsDto>> Handle(
        GetMonitoringStatsQuery request, CancellationToken ct)
    {
        var result = await queryService.GetMonitoringStatsAsync(request, ct);
        return ApiResponse<MonitoringStatsDto>.Success(result);
    }
}
