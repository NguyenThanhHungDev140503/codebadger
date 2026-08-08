namespace Application.Features.CentralDbSync.Models;

/// <summary>
/// Durable CT dispatch outbox marker for scalable bootstrap.
/// One marker per successful final publish ensures exactly one CT continuation
/// runs from C1. Dispatched by <c>BootstrapCtDispatchService</c>.
/// </summary>
public sealed record BootstrapCtDispatch
{
    public Guid DispatchId { get; init; }
    public required string RuleName { get; init; }
    public Guid ParentId { get; init; }
    public long Watermark { get; init; }
    public required string Status { get; init; }
    public int AttemptCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DispatchLeaseUntil { get; init; }
    public DateTime? DispatchedAt { get; init; }
    public string? HangfireJobId { get; init; }
    public Guid? DispatchLeaseToken { get; init; }
    public string? LastError { get; init; }
}
