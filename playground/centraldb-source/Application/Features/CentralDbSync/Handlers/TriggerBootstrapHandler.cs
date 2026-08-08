using Application.Common.Models;
using Application.Features.CentralDbSync.Commands;
using Application.Features.CentralDbSync.Dtos;
using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Services;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Features.CentralDbSync.Handlers;

public sealed class TriggerBootstrapHandler(
    IMappingRuleProvider ruleProvider,
    BootstrapRequestService requestService)
    : IRequestHandler<TriggerBootstrapCommand, ApiResponse<BootstrapResponseDto>>
{
    public async Task<ApiResponse<BootstrapResponseDto>> Handle(
        TriggerBootstrapCommand request, CancellationToken ct)
    {
        // Rule registration is enforced by TriggerBootstrapCommandValidator in the
        // MediatR pipeline, so Get cannot throw here.
        var rule = ruleProvider.Get(request.RuleName);

        var result = await requestService.SubmitAsync(request.RuleName, ct);

        if (result.Request.ErrorCode == "ActiveScalableBootstrapExists")
        {
            return ApiResponse<BootstrapResponseDto>.Failure(
                result.Request.ErrorMessage,
                StatusCodes.Status409Conflict);
        }

        var dto = new BootstrapResponseDto(
            result.Request.RequestId,
            result.Request.HangfireJobId,
            request.RuleName,
            rule.Source.PrimaryTable,
            result.Request.Status,
            StatusUrl: null);

        // Bootstrap is queued, not completed → 202.
        return new ApiResponse<BootstrapResponseDto>(
            successed: true,
            statusCode: StatusCodes.Status202Accepted,
            data: dto);
    }
}
