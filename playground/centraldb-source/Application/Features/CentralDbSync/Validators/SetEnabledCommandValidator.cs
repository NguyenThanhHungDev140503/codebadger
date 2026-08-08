using Application.Features.CentralDbSync.Commands;
using Application.Features.CentralDbSync.Mapping;
using FluentValidation;

namespace Application.Features.CentralDbSync.Validators;

/// <summary>
/// Rejects an enable/disable command whose rule name is absent from the mapping registry.
/// </summary>
public sealed class SetEnabledCommandValidator : AbstractValidator<SetEnabledCommand>
{
    public SetEnabledCommandValidator(IMappingRuleProvider ruleProvider)
    {
        RuleFor(x => x.RuleName)
            .NotEmpty()
            .Must(name => ruleProvider.TryGet(name, out _))
            .WithMessage(x => $"Rule '{x.RuleName}' is not registered.");
    }
}
