namespace Application.Features.CentralDbSync.Models;

/// <summary>
/// Encapsulates the durable child snapshot used by recovery claim operations:
/// child identity, parent identity, parent fencing token, expected status,
/// last known Hangfire job id, claim token, and stale-claim cutoff.
/// </summary>
public sealed record BootstrapChildRecoveryExpectation(
    Guid ChildId,
    Guid ParentId,
    Guid FencingToken,
    string ExpectedStatus,
    string? ExpectedJobId,
    string ClaimToken,
    DateTime StaleClaimBeforeUtc);
