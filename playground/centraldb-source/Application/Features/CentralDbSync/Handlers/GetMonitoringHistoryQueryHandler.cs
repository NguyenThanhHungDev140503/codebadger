using Application.Common.Models;
using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Dtos;
using Application.Features.CentralDbSync.Queries;
using MediatR;

namespace Application.Features.CentralDbSync.Handlers;

public sealed class GetMonitoringHistoryQueryHandler(ICentralDbSyncQueryService queryService)
    : IRequestHandler<GetMonitoringHistoryQuery, ApiResponse<List<MonitoringHistoryPointDto>>>
{
    public async Task<ApiResponse<List<MonitoringHistoryPointDto>>> Handle(
        GetMonitoringHistoryQuery request, CancellationToken ct)
    {
        var result = await queryService.GetMonitoringHistoryAsync(request, ct);
        return ApiResponse<List<MonitoringHistoryPointDto>>.Success(result.ToList());
    }
}
