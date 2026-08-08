using System.Diagnostics.Metrics;

namespace Application.Features.CentralDbSync.Models;

public static class BootstrapLifecycleMetrics
{
    public static readonly Meter Meter = new("CentralDbSync.Bootstrap", "1.0");
    public static readonly Counter<long> CasLost = Meter.CreateCounter<long>("cas_lost");
    public static readonly Counter<long> ClaimFinalized = Meter.CreateCounter<long>("claim_finalized");
    public static readonly Counter<long> RecoverySucceeded = Meter.CreateCounter<long>("recovery_succeeded");
    public static readonly Counter<long> RecoveryScheduleFailed = Meter.CreateCounter<long>("recovery_schedule_failed");
    public static readonly Counter<long> RecoveryExhausted = Meter.CreateCounter<long>("recovery_exhausted");
    public static readonly Counter<long> StaleWorkerStopped = Meter.CreateCounter<long>("stale_worker_stopped");
}
