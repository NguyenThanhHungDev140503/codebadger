using Application.Features.CentralDbSync.Commands;
using Application.Features.CentralDbSync.Mapping;
using FluentValidation;

namespace Application.Features.CentralDbSync.Validators;

/// <summary>
/// Rejects a bootstrap command whose rule name is absent from the mapping registry.
/// Bootstrap is triggered manually by an operator, so the message lists the valid names.
/// </summary>
public sealed class TriggerBootstrapCommandValidator : AbstractValidator<TriggerBootstrapCommand>
{
    public TriggerBootstrapCommandValidator(IMappingRuleProvider ruleProvider)
    {
        RuleFor(x => x.RuleName)
            .NotEmpty()
            .Must(name => ruleProvider.TryGet(name, out _))
            .WithMessage(x =>
            {
                var allowed = string.Join(", ", ruleProvider.GetAll().Select(r => r.RuleName));
                return $"Rule '{x.RuleName}' is not registered. Allowed: {allowed}";
            });
    }
}
