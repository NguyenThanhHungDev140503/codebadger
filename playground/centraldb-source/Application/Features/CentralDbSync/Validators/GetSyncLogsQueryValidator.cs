using Application.Common.Models;
using Application.Features.CentralDbSync.Queries;
using FluentValidation;

namespace Application.Features.CentralDbSync.Validators;

public sealed class GetSyncLogsQueryValidator : PaginationRequestValidator<GetSyncLogsQuery>
{
    private static readonly HashSet<string> AllowedOutcomes = new(StringComparer.OrdinalIgnoreCase)
    {
        "succeeded",
        "no_changes",
        "failed",
        "skipped_locked",
        "skipped_dependency",
        "requires_full_resync"
    };

    public GetSyncLogsQueryValidator()
    {
        RuleFor(x => x.Outcome)
            .Must(outcome => string.IsNullOrEmpty(outcome) || AllowedOutcomes.Contains(outcome))
            .WithMessage($"Outcome must be one of: {string.Join(", ", AllowedOutcomes)}.");

        RuleFor(x => x)
            .Must(x => !x.From.HasValue || !x.To.HasValue || x.To.Value >= x.From.Value)
            .WithMessage("'To' date must be greater than or equal to 'From' date.")
            .WithName("To");
    }
}
