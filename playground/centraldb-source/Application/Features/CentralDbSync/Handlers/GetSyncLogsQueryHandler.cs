using Application.Common.Models;
using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Dtos;
using Application.Features.CentralDbSync.Queries;
using MediatR;

namespace Application.Features.CentralDbSync.Handlers;

public sealed class GetSyncLogsQueryHandler(ICentralDbSyncQueryService queryService)
    : IRequestHandler<GetSyncLogsQuery, ApiResponse<PaginatedResponse<SyncRunLogDto>>>
{
    public async Task<ApiResponse<PaginatedResponse<SyncRunLogDto>>> Handle(
        GetSyncLogsQuery request, CancellationToken ct)
    {
        var result = await queryService.GetLogsAsync(request, ct);
        return ApiResponse<PaginatedResponse<SyncRunLogDto>>.Success(result);
    }
}
