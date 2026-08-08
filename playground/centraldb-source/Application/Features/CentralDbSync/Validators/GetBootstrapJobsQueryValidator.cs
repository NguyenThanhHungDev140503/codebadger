using Application.Common.Models;
using Application.Features.CentralDbSync.Queries;
using FluentValidation;

namespace Application.Features.CentralDbSync.Validators;

public sealed class GetBootstrapJobsQueryValidator : PaginationRequestValidator<GetBootstrapJobsQuery>
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pending",
        "Running",
        "Success",
        "Failed"
    };

    public GetBootstrapJobsQueryValidator()
    {
        RuleFor(x => x.RuleName)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.RuleName));

        RuleFor(x => x.Status)
            .Must(status => string.IsNullOrWhiteSpace(status) || AllowedStatuses.Contains(status))
            .WithMessage("Status must be one of: Pending, Running, Success, Failed.");
    }
}
