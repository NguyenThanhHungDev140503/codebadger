using Application.Features.CentralDbSync.Queries;
using FluentValidation;

namespace Application.Features.CentralDbSync.Validators;

public sealed class GetMonitoringStatsQueryValidator : AbstractValidator<GetMonitoringStatsQuery>
{
    public GetMonitoringStatsQueryValidator()
    {
        RuleFor(x => x)
            .Must(x => !x.From.HasValue || !x.To.HasValue || x.To.Value >= x.From.Value)
            .WithMessage("'To' date must be greater than or equal to 'From' date.")
            .WithName("To");
    }
}
