namespace Application.Features.CentralDbSync.Models;

public static class BootstrapRecoverySchedulePolicy
{
    public const int MaxSuccessfulRecoveries = 3;
    public const int MaxConsecutiveScheduleFailures = 3;
    public static readonly TimeSpan[] Backoff =
    [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15)];

    public static TimeSpan GetBackoff(int priorFailures) =>
        Backoff[Math.Clamp(priorFailures, 0, Backoff.Length - 1)];
}
