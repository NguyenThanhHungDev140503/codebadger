namespace Application.Features.CentralDbSync.Models;

/// <summary>
/// Encapsulates the compare-and-swap guard fields used by recovery store operations:
/// the request identity, its expected durable status, the last known Hangfire job id,
/// and the current recovery attempt counter.
/// </summary>
public sealed record BootstrapRecoveryExpectation(
    Guid RequestId,
    string ExpectedStatus,
    string ExpectedHangfireJobId,
    int ExpectedReconcileAttemptCount,
    string? ExpectedClaimToken = null);
