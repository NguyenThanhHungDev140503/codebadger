using Application.Common.Models;
using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Commands;
using MediatR;

namespace Application.Features.CentralDbSync.Handlers;

public sealed class CancelBootstrapParentHandler(IBootstrapMonitorActionService actionService)
    : IRequestHandler<CancelBootstrapParentCommand, ApiResponse<BootstrapMonitorActionResult>>
{
    public async Task<ApiResponse<BootstrapMonitorActionResult>> Handle(
        CancelBootstrapParentCommand request, CancellationToken ct)
    {
        var result = await actionService.RequestCancelAsync(request.ParentId, "api", ct);

        return ReconcileBootstrapParentHandler.MapActionResult(result);
    }
}
