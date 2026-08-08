using Application.Common.Models;
using Application.Features.CentralDbSync.Abstractions;
using MediatR;

namespace Application.Features.CentralDbSync.Commands;

public sealed record CancelBootstrapParentCommand(Guid ParentId)
    : IRequest<ApiResponse<BootstrapMonitorActionResult>>;
