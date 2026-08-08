using Application.Common.Models;
using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Dtos;
using Application.Features.CentralDbSync.Queries;
using MediatR;

namespace Application.Features.CentralDbSync.Handlers;

public sealed class GetBootstrapDiagnosticTimelineQueryHandler(IBootstrapMonitorQueryService queryService)
    : IRequestHandler<GetBootstrapDiagnosticTimelineQuery, ApiResponse<IReadOnlyList<BootstrapDiagnosticEventDto>>>
{
    public async Task<ApiResponse<IReadOnlyList<BootstrapDiagnosticEventDto>>> Handle(
        GetBootstrapDiagnosticTimelineQuery request, CancellationToken ct)
    {
        var timeline = await queryService.GetTimelineAsync(
            request.RequestId, request.PageIndex, request.PageSize, ct);

        return ApiResponse<IReadOnlyList<BootstrapDiagnosticEventDto>>.Success(timeline);
    }
}
