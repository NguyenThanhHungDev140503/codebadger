using Application.Common.Models;
using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Commands;
using MediatR;

namespace Application.Features.CentralDbSync.Handlers;

public sealed class RetryBootstrapChildHandler(IBootstrapMonitorActionService actionService)
    : IRequestHandler<RetryBootstrapChildCommand, ApiResponse<BootstrapMonitorActionResult>>
{
    public async Task<ApiResponse<BootstrapMonitorActionResult>> Handle(
        RetryBootstrapChildCommand request, CancellationToken ct)
    {
        var result = await actionService.RetryAsync(
            BootstrapMonitorTarget.Child(request.ParentId, request.ChildId), "api", ct);

        return ReconcileBootstrapParentHandler.MapActionResult(result);
    }
}
