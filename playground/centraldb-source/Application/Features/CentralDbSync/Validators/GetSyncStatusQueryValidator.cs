using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Queries;
using FluentValidation;

namespace Application.Features.CentralDbSync.Validators;

/// <summary>
/// Rejects a sync-status query whose rule name is absent from the mapping registry.
/// </summary>
public sealed class GetSyncStatusQueryValidator : AbstractValidator<GetSyncStatusQuery>
{
    public GetSyncStatusQueryValidator(IMappingRuleProvider ruleProvider)
    {
        RuleFor(x => x.RuleName)
            .NotEmpty()
            .Must(name => ruleProvider.TryGet(name, out _))
            .WithMessage(x => $"Rule '{x.RuleName}' is not registered.");

        RuleFor(x => x.MaxAllowedLagMinutes)
            .GreaterThan(0)
            .When(x => x.MaxAllowedLagMinutes.HasValue);
    }
}
