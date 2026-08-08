using Application.Common.Models;
using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Dtos;
using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Queries;
using MediatR;

namespace Application.Features.CentralDbSync.Handlers;

public sealed class GetSyncRulesHandler(
    IMappingRuleProvider ruleProvider,
    ISyncConfigStore configStore)
    : IRequestHandler<GetSyncRulesQuery, ApiResponse<PaginatedResponse<SyncRuleDto>>>
{
    public async Task<ApiResponse<PaginatedResponse<SyncRuleDto>>> Handle(
        GetSyncRulesQuery request, CancellationToken ct)
    {
        // A rule absent from the store has never been seeded, so the recurring job
        // skips it. Report false to match ISyncConfigStore.IsEnabledAsync.
        var configured = await configStore.GetAllConfiguredAsync(ct);

        var rules = ruleProvider.GetAll().Select(rule =>
        {
            var config = rule.ToTableSyncConfig();
            var enabled = configured.TryGetValue(rule.RuleName, out var persisted) && persisted;
            return new SyncRuleDto(
                rule.RuleName,
                config.TargetTable,
                config.SyncMode,
                enabled);
        }).ToList();

        var paged = PaginatedResponse<SyncRuleDto>.Create(
            rules,
            request.PageIndex,
            request.PageSize);

        return ApiResponse<PaginatedResponse<SyncRuleDto>>.Success(paged);
    }
}
