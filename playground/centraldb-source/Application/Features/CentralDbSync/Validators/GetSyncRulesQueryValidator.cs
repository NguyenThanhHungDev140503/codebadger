using Application.Common.Models;
using Application.Features.CentralDbSync.Queries;

namespace Application.Features.CentralDbSync.Validators;

public sealed class GetSyncRulesQueryValidator : PaginationRequestValidator<GetSyncRulesQuery>;
