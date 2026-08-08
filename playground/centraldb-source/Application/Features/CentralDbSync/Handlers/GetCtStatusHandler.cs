using Application.Common.Models;
using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Dtos;
using Application.Features.CentralDbSync.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Features.CentralDbSync.Handlers;

public sealed class GetCtStatusHandler(ISqlServerCtHealthCheck ctHealthCheck)
    : IRequestHandler<GetCtStatusQuery, ApiResponse<CtStatusDto>>
{
    public async Task<ApiResponse<CtStatusDto>> Handle(
        GetCtStatusQuery request, CancellationToken ct)
    {
        // Rule registration is enforced by GetCtStatusQueryValidator in the MediatR pipeline.
        var result = await ctHealthCheck.CheckAsync(request.RuleName, ct);

        // The rule is known to exist, so any error here is an infrastructure fault
        // (connection failure, missing permission, table dropped in the source DB).
        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            return ApiResponse<CtStatusDto>.Failure(
                result.ErrorMessage,
                StatusCodes.Status500InternalServerError);
        }

        var dto = new CtStatusDto(
            result.SourceTable,
            result.SchemaQualifiedName,
            result.IsCtEnabled,
            result.CurrentVersion,
            result.MinValidVersion);

        return ApiResponse<CtStatusDto>.Success(dto);
    }
}
