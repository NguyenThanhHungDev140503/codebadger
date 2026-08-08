using Application.Common.Models;
using Application.Features.CentralDbSync.Dtos;
using MediatR;

namespace Application.Features.CentralDbSync.Queries;

public sealed record GetSyncStatusQuery(string RuleName, int? MaxAllowedLagMinutes = null)
    : IRequest<ApiResponse<SyncStatusDto>>;
