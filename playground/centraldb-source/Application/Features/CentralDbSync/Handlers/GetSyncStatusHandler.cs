using Application.Common.Models;
using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Dtos;
using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Models;
using Application.Features.CentralDbSync.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Features.CentralDbSync.Handlers;

public sealed class GetSyncStatusHandler(
    ISyncCheckpointStore checkpointStore,
    IMappingRuleProvider ruleProvider)
    : IRequestHandler<GetSyncStatusQuery, ApiResponse<SyncStatusDto>>
{
    private static readonly TimeSpan HotMaxAllowedLag = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ColdMaxAllowedLag = TimeSpan.FromHours(1);

    public async Task<ApiResponse<SyncStatusDto>> Handle(
        GetSyncStatusQuery request, CancellationToken ct)
    {
        // Rule registration is enforced by GetSyncStatusQueryValidator in the MediatR pipeline.
        var state = await checkpointStore.GetAsync(request.RuleName, ct);

        // No checkpoint yet means the rule exists but has not been bootstrapped/seeded.
        // Treat it as a state conflict, not a missing endpoint/resource.
        if (state is null)
        {
            return ApiResponse<SyncStatusDto>.Failure(
                $"No sync checkpoint found for rule '{request.RuleName}'.",
                StatusCodes.Status409Conflict);
        }

        var rule = ruleProvider.Get(request.RuleName);
        var maxAllowedLag = request.MaxAllowedLagMinutes.HasValue
            ? TimeSpan.FromMinutes(request.MaxAllowedLagMinutes.Value)
            : GetMaxAllowedLag(rule.SyncTier);

        var status = state.SyncStatus switch
        {
            SyncStatus.CheckpointState.PendingInitialSync => "Unknown",
            SyncStatus.CheckpointState.RequiresFullResync => "Degraded",
            _ when state.ConsecutiveFailureCount > 0 => "Degraded",
            _ when state.LastSuccessAt.HasValue &&
                   DateTime.UtcNow - state.LastSuccessAt.Value
                       >= maxAllowedLag => "Degraded",
            _ => "Healthy"
        };

        return ApiResponse<SyncStatusDto>.Success(new SyncStatusDto(
            request.RuleName,
            status,
            state.LastSyncVersion,
            state.LastSuccessAt,
            state.ConsecutiveFailureCount,
            state.SyncStatus));
    }

    private static TimeSpan GetMaxAllowedLag(string syncTier)
        => string.Equals(syncTier, "Hot", StringComparison.OrdinalIgnoreCase)
            ? HotMaxAllowedLag
            : ColdMaxAllowedLag;
}
