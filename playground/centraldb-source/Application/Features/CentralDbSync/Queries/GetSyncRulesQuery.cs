using Application.Common.Models;
using Application.Features.CentralDbSync.Dtos;

namespace Application.Features.CentralDbSync.Queries;

public sealed class GetSyncRulesQuery
    : PaginationRequest<PaginatedResponse<SyncRuleDto>>;
