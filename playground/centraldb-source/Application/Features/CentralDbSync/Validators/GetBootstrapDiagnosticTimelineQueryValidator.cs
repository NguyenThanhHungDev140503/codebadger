using Application.Features.CentralDbSync.Queries;
using FluentValidation;

namespace Application.Features.CentralDbSync.Validators;

public sealed class GetBootstrapDiagnosticTimelineQueryValidator : AbstractValidator<GetBootstrapDiagnosticTimelineQuery>
{
    public GetBootstrapDiagnosticTimelineQueryValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty();

        RuleFor(x => x.PageIndex)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(200)
            .WithMessage("PageSize must be between 1 and 200.");
    }
}
