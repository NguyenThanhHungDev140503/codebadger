using Application.Common.Models;
using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Commands;
using Application.Features.CentralDbSync.Services;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Features.CentralDbSync.Handlers;

public sealed class ReconcileBootstrapHandler(
    BootstrapRequestService requestService)
    : IRequestHandler<ReconcileBootstrapCommand, ApiResponse<object>>
{
    public async Task<ApiResponse<object>> Handle(
        ReconcileBootstrapCommand request, CancellationToken ct)
    {
        await requestService.ReconcileStaleAsync(DateTime.UtcNow, ct);

        return new ApiResponse<object>(
            successed: true,
            statusCode: StatusCodes.Status200OK,
            data: null);
    }
}
