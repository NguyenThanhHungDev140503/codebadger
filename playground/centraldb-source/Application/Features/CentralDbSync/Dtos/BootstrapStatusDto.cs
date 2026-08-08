namespace Application.Features.CentralDbSync.Dtos;

/// <summary>
/// DTO exposing bootstrap request status. Includes optional scalable parent-child fields
/// when <see cref="BootstrapType"/> is <c>"scalable"</c>.
/// </summary>
public sealed record BootstrapStatusDto
{
    // Bootstrap request fields (both flows)
    public required Guid RequestId { get; init; }
    public required string RuleName { get; init; }
    public required string SourceTable { get; init; }
    public required string Status { get; init; }
    public required string BootstrapType { get; init; }
    public string? HangfireJobId { get; init; }
    public long RowsStaged { get; init; }
    public long? TotalRowsExpected { get; init; }
    public int AttemptCount { get; init; }
    public int ReconcileAttemptCount { get; init; }
    public DateTime RequestedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? FinishedAt { get; init; }
    public DateTime? FirstRecoveryAt { get; init; }
    public DateTime? LastRecoveryAt { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    // Scalable bootstrap fields (only populated when BootstrapType == "scalable")
    public Guid? ParentId { get; init; }
    public string? ParentStatus { get; init; }
    public int ChildrenCompleted { get; init; }
    public int ChildrenTotal { get; init; }
    public long? BaselineVersion { get; init; }
    public long? WatermarkVersion { get; init; }
    public string? StagingTableName { get; init; }
    public string? LastProcessedKey { get; init; }
    public bool DeferredCtPending { get; init; }
}
