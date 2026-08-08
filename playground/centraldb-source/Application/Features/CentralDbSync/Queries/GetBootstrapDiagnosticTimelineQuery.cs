using Application.Common.Models;
using Application.Features.CentralDbSync.Dtos;
using MediatR;

namespace Application.Features.CentralDbSync.Queries;

public sealed record GetBootstrapDiagnosticTimelineQuery(
    Guid RequestId,
    int PageIndex = 1,
    int PageSize = 50)
    : IRequest<ApiResponse<IReadOnlyList<BootstrapDiagnosticEventDto>>>;
