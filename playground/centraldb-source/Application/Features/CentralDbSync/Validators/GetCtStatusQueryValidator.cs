using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Queries;
using FluentValidation;

namespace Application.Features.CentralDbSync.Validators;

/// <summary>
/// Rejects a ct-status query whose rule name is absent from the mapping registry.
/// Runs in ValidationBehavior before the handler, so the handler never sees an unknown rule.
/// </summary>
public sealed class GetCtStatusQueryValidator : AbstractValidator<GetCtStatusQuery>
{
    public GetCtStatusQueryValidator(IMappingRuleProvider ruleProvider)
    {
        RuleFor(x => x.RuleName)
            .NotEmpty()
            .Must(name => ruleProvider.TryGet(name, out _))
            .WithMessage(x => $"Rule '{x.RuleName}' is not registered.");
    }
}
