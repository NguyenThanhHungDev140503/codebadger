namespace Infrastructure.CentralDbSync;

using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Models;
using Microsoft.Extensions.Options;

/// <summary>
/// Binds <see cref="CentralDbSyncOptions"/> to the Application-layer
/// <see cref="IBootstrapReconciliationPolicy"/> contract.
/// </summary>
public sealed class BootstrapReconciliationPolicy : IBootstrapReconciliationPolicy
{
    public BootstrapReconciliationPolicy(IOptions<CentralDbSyncOptions> options)
    {
        var o = options.Value;
        IdleAfter = o.BootstrapIdleReconciliationAfter;
        RunningStaleAfter = o.BootstrapRunningStaleAfter;
        WaitingForLockStaleAfter = o.BootstrapWaitingForLockStaleAfter;
        MaxRecoveryAttempts = BootstrapRecoveryConstants.MaxRecoveryAttempts;
    }

    public TimeSpan IdleAfter { get; }
    public TimeSpan RunningStaleAfter { get; }
    public TimeSpan WaitingForLockStaleAfter { get; }
    public int MaxRecoveryAttempts { get; }
}
