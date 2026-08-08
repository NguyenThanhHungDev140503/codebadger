namespace Application.Features.CentralDbSync.Models;

/// <summary>
/// Result of idempotently creating the next child in a parent chain.
/// Only the caller that created the row owns enqueueing its first job.
/// </summary>
public sealed record BootstrapNextChildResult(BootstrapChild Child, bool WasCreated);
