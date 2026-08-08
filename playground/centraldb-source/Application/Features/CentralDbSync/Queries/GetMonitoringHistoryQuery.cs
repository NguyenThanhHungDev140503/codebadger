using Application.Common.Models;
using Application.Features.CentralDbSync.Dtos;
using MediatR;

namespace Application.Features.CentralDbSync.Queries;

public sealed record GetMonitoringHistoryQuery(
    DateTime? From,
    DateTime? To,
    int BucketMinutes = 60)
    : IRequest<ApiResponse<List<MonitoringHistoryPointDto>>>;
