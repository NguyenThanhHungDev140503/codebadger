using Application.Common.Models;
using Application.Features.CentralDbSync.Queries;
using FluentValidation;

namespace Application.Features.CentralDbSync.Validators;

public sealed class GetBootstrapMonitorListQueryValidator : PaginationRequestValidator<GetBootstrapMonitorListQuery>
{
    public GetBootstrapMonitorListQueryValidator()
    {
        RuleFor(x => x.RuleName)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.RuleName));

        RuleFor(x => x.Status)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Status));
    }
}
