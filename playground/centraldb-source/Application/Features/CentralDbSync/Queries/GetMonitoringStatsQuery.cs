using Application.Common.Models;
using Application.Features.CentralDbSync.Dtos;
using MediatR;

namespace Application.Features.CentralDbSync.Queries;

public sealed record GetMonitoringStatsQuery(
    DateTime? From,
    DateTime? To)
    : IRequest<ApiResponse<MonitoringStatsDto>>;
