using Application.Features.CentralDbSync.Queries;
using FluentValidation;

namespace Application.Features.CentralDbSync.Validators;

public sealed class GetMonitoringHistoryQueryValidator : AbstractValidator<GetMonitoringHistoryQuery>
{
    public GetMonitoringHistoryQueryValidator()
    {
        RuleFor(x => x.BucketMinutes)
            .InclusiveBetween(1, 1440);

        RuleFor(x => x)
            .Must(x => !x.From.HasValue || !x.To.HasValue || x.To.Value >= x.From.Value)
            .WithMessage("'To' date must be greater than or equal to 'From' date.")
            .WithName("To");
    }
}
