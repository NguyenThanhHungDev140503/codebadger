using Application.Common.Models;
using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Commands;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Features.CentralDbSync.Handlers;

public sealed class ReconcileBootstrapParentHandler(IBootstrapMonitorActionService actionService)
    : IRequestHandler<ReconcileBootstrapParentCommand, ApiResponse<BootstrapMonitorActionResult>>
{
    public async Task<ApiResponse<BootstrapMonitorActionResult>> Handle(
        ReconcileBootstrapParentCommand request, CancellationToken ct)
    {
        var result = await actionService.ReconcileAsync(
            BootstrapMonitorTarget.Parent(request.ParentId), "api", ct);

        return MapActionResult(result);
    }

    public static ApiResponse<BootstrapMonitorActionResult> MapActionResult(
        BootstrapMonitorActionResult result) => result.Status switch
    {
        "accepted" => new ApiResponse<BootstrapMonitorActionResult>(
            true, StatusCodes.Status202Accepted, result),
        "not_found" => ApiResponse<BootstrapMonitorActionResult>.Failure(
            result.Message, StatusCodes.Status404NotFound),
        "conflict" => ApiResponse<BootstrapMonitorActionResult>.Failure(
            result.Message, StatusCodes.Status409Conflict),
        _ => ApiResponse<BootstrapMonitorActionResult>.Failure(
            result.Message, StatusCodes.Status503ServiceUnavailable)
    };
}
