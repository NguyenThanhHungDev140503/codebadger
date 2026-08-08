using Application.Common.Models;
using Application.Features.CentralDbSync.Dtos;
using MediatR;

namespace Application.Features.CentralDbSync.Queries;

public sealed record GetCtStatusQuery(string RuleName)
    : IRequest<ApiResponse<CtStatusDto>>;
