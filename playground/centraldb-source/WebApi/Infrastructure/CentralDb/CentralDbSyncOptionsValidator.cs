
using Cronos;
using Infrastructure.CentralDbSync;
using Microsoft.Extensions.Options;

namespace WebApi.Infrastructure.CentralDb;
/// <summary>
/// Validates <see cref="CentralDbSyncOptions"/> reconciliation settings at startup.
/// to verify that <see cref="CentralDbSyncOptions.BootstrapReconciliationCron"/>
/// is a Hangfire-compatible five-field cron expression.
/// </summary>
public sealed class CentralDbSyncOptionsValidator : IValidateOptions<CentralDbSyncOptions>
{
    public ValidateOptionsResult Validate(string? name, CentralDbSyncOptions options)
    {
        var errors = new List<string>();

        if (options.BootstrapIdleReconciliationAfter <= TimeSpan.Zero)
            errors.Add(
                $"BootstrapIdleReconciliationAfter must be greater than zero (current: {options.BootstrapIdleReconciliationAfter}).");

        if (options.BootstrapRunningStaleAfter <= TimeSpan.Zero)
            errors.Add(
                $"BootstrapRunningStaleAfter must be greater than zero (current: {options.BootstrapRunningStaleAfter}).");

        if (options.BootstrapWaitingForLockStaleAfter <= TimeSpan.Zero)
            errors.Add(
                $"BootstrapWaitingForLockStaleAfter must be greater than zero (current: {options.BootstrapWaitingForLockStaleAfter}).");

        ValidateCron(errors, options.BootstrapReconciliationCron);

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }

    private static void ValidateCron(List<string> errors, string cron)
    {
        if (string.IsNullOrWhiteSpace(cron))
        {
            errors.Add("BootstrapReconciliationCron must not be null, empty, or whitespace.");
            return;
        }

        if (!CronExpression.TryParse(cron.Trim(), CronFormat.Standard, out _))
        {
            errors.Add(
                $"BootstrapReconciliationCron '{cron.Trim()}' is not a valid Hangfire five-field cron expression.");
        }
    }
}
