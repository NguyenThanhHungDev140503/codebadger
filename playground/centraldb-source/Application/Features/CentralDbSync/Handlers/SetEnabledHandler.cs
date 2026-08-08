using Application.Common.Models;
using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Commands;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Features.CentralDbSync.Handlers;

public sealed class SetEnabledHandler(ISyncConfigStore configStore)
    : IRequestHandler<SetEnabledCommand, ApiResponse<object>>
{
    public async Task<ApiResponse<object>> Handle(
        SetEnabledCommand request, CancellationToken ct)
    {
        // Rule registration is enforced by SetEnabledCommandValidator in the MediatR pipeline.
        try
        {
            await configStore.SetEnabledAsync(request.RuleName, request.Enabled, ct);
        }
        catch (InvalidOperationException)
        {
            // The rule is registered but has no config row yet — it is seeded on bootstrap success.
            return ApiResponse<object>.Failure(
                $"Rule '{request.RuleName}' has no sync config row yet. Run bootstrap first.",
                StatusCodes.Status409Conflict);
        }

        var status = request.Enabled ? "enabled" : "disabled";
        return ApiResponse<object>.Success(
            msg: $"Rule '{request.RuleName}' is now {status}.");
    }
}
