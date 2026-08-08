using Application.Common.Models;
using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Dtos;
using Application.Features.CentralDbSync.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Features.CentralDbSync.Handlers;

public sealed class GetBootstrapMonitorDetailQueryHandler(IBootstrapMonitorQueryService queryService)
    : IRequestHandler<GetBootstrapMonitorDetailQuery, ApiResponse<BootstrapMonitorDetailDto>>
{
    public async Task<ApiResponse<BootstrapMonitorDetailDto>> Handle(
        GetBootstrapMonitorDetailQuery request, CancellationToken ct)
    {
        var detail = await queryService.GetDetailAsync(request.RequestId, ct);

        if (detail is null)
        {
            return ApiResponse<BootstrapMonitorDetailDto>.Failure(
                $"Bootstrap request {request.RequestId} not found.",
                StatusCodes.Status404NotFound);
        }

        return ApiResponse<BootstrapMonitorDetailDto>.Success(detail);
    }
}
