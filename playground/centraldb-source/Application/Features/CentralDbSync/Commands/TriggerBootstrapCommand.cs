using Application.Common.Models;
using Application.Features.CentralDbSync.Dtos;
using MediatR;

namespace Application.Features.CentralDbSync.Commands;

public sealed record TriggerBootstrapCommand(string RuleName)
    : IRequest<ApiResponse<BootstrapResponseDto>>;
