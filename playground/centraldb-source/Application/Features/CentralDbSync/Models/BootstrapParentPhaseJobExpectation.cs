namespace Application.Features.CentralDbSync.Models;

/// <summary>
/// Encapsulates the durable parent phase-job snapshot used by claim operations:
/// parent identity, fencing token, expected status, expected old phase job id,
/// claim token, and stale-claim cutoff.
/// </summary>
public sealed record BootstrapParentPhaseJobExpectation(
    Guid ParentId,
    Guid FencingToken,
    string ExpectedStatus,
    string? ExpectedPhaseJobId,
    string ClaimToken,
    DateTime StaleClaimBeforeUtc);
