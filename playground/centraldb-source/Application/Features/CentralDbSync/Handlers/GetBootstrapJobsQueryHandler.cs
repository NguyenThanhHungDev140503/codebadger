using Application.Common.Models;
using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Dtos;
using Application.Features.CentralDbSync.Queries;
using MediatR;

namespace Application.Features.CentralDbSync.Handlers;

public sealed class GetBootstrapJobsQueryHandler(ICentralDbSyncQueryService queryService)
    : IRequestHandler<GetBootstrapJobsQuery, ApiResponse<PaginatedResponse<BootstrapJobListItemDto>>>
{
    public async Task<ApiResponse<PaginatedResponse<BootstrapJobListItemDto>>> Handle(
        GetBootstrapJobsQuery request, CancellationToken ct)
    {
        var result = await queryService.GetBootstrapJobsAsync(request, ct);
        return ApiResponse<PaginatedResponse<BootstrapJobListItemDto>>.Success(result);
    }
}
