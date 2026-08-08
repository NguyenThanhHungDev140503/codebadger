using Application.Common.Models;
using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Dtos;
using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Models;
using Application.Features.CentralDbSync.Queries;
using Application.Features.CentralDbSync.Services;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Features.CentralDbSync.Handlers;

public sealed class GetBootstrapStatusHandler(
    BootstrapRequestService requestService,
    IBootstrapParentStore parentStore,
    IBootstrapChildStore childStore,
    IMappingRuleProvider ruleProvider)
    : IRequestHandler<GetBootstrapStatusQuery, ApiResponse<BootstrapStatusDto>>
{
    public async Task<ApiResponse<BootstrapStatusDto>> Handle(
        GetBootstrapStatusQuery request, CancellationToken ct)
    {
        var bootstrapRequest = await requestService.GetStatusAsync(request.RequestId, ct);
        if (bootstrapRequest is null)
        {
            return ApiResponse<BootstrapStatusDto>.Failure(
                $"Bootstrap request {request.RequestId} not found.",
                StatusCodes.Status404NotFound);
        }

        // bootstrapRequest.SourceTable stores the RuleName; resolve it to the real
        // source table.
        var sourceTable = ruleProvider.TryGet(bootstrapRequest.SourceTable, out var rule)
            ? rule.Source.PrimaryTable
            : bootstrapRequest.SourceTable;

        var dto = new BootstrapStatusDto
        {
            RequestId = bootstrapRequest.RequestId,
            RuleName = bootstrapRequest.SourceTable,
            SourceTable = sourceTable,
            Status = bootstrapRequest.Status,
            BootstrapType = bootstrapRequest.BootstrapType,
            HangfireJobId = bootstrapRequest.HangfireJobId,
            RowsStaged = bootstrapRequest.RowsStaged,
            TotalRowsExpected = bootstrapRequest.TotalRowsExpected,
            AttemptCount = bootstrapRequest.AttemptCount,
            ReconcileAttemptCount = bootstrapRequest.ReconcileAttemptCount,
            RequestedAt = bootstrapRequest.RequestedAt,
            UpdatedAt = bootstrapRequest.UpdatedAt,
            StartedAt = bootstrapRequest.StartedAt,
            FinishedAt = bootstrapRequest.FinishedAt,
            FirstRecoveryAt = bootstrapRequest.FirstRecoveryAt,
            LastRecoveryAt = bootstrapRequest.LastRecoveryAt,
            ErrorCode = bootstrapRequest.ErrorCode,
            ErrorMessage = bootstrapRequest.ErrorMessage
        };

        // For scalable bootstrap requests, enrich with parent/child progress
        if (bootstrapRequest.BootstrapType == BootstrapRequestType.Scalable)
        {
            var parent = await parentStore.GetByRuleNameAsync(bootstrapRequest.SourceTable, ct);
            if (parent is not null)
            {
                var children = await childStore.GetByParentAsync(parent.ParentId, ct);
                var completedChildren = children.Count(c => c.Status == BootstrapChildStatus.Completed);

                dto = dto with
                {
                    ParentId = parent.ParentId,
                    ParentStatus = parent.Status,
                    ChildrenCompleted = completedChildren,
                    ChildrenTotal = children.Count,
                    BaselineVersion = parent.BaselineVersion,
                    WatermarkVersion = parent.WatermarkVersion,
                    StagingTableName = parent.StagingTableName is not null
                        ? $"{parent.StagingSchema}.{parent.StagingTableName}"
                        : null,
                    LastProcessedKey = parent.LastProcessedKey,
                    DeferredCtPending = parent.DeferredCtPending
                };
            }
        }

        return ApiResponse<BootstrapStatusDto>.Success(dto);
    }
}
