using Application.Common.Models;
using Application.Features.CentralDbSync.Abstractions;
using MediatR;

namespace Application.Features.CentralDbSync.Commands;

public sealed record RetryBootstrapChildCommand(Guid ParentId, Guid ChildId)
    : IRequest<ApiResponse<BootstrapMonitorActionResult>>;
