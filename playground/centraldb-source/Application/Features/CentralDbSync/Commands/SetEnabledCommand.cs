using Application.Common.Models;
using MediatR;

namespace Application.Features.CentralDbSync.Commands;

public sealed record SetEnabledCommand(string RuleName, bool Enabled)
    : IRequest<ApiResponse<object>>;
