namespace Application.Features.CentralDbSync.Dtos;

public sealed record BootstrapMonitorDetailDto
{
    public Guid RequestId { get; init; }
    public string RuleName { get; init; } = string.Empty;
    public string RequestStatus { get; init; } = string.Empty;
    public string? BootstrapType { get; init; }
    public DateTime? CreatedAt { get; init; }
    public MonitorParentDto? Parent { get; init; }
    public List<MonitorChildDto> Children { get; init; } = [];
    public List<BootstrapDiagnosticEventDto> Timeline { get; init; } = [];
}

public sealed record MonitorParentDto
{
    public Guid ParentId { get; init; }
    public string RuleName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? StagingTableName { get; init; }
    public long? BaselineVersion { get; init; }
    public long? WatermarkVersion { get; init; }
    public long RowsStaged { get; init; }
    public int AttemptCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastHeartbeatAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? PhaseJobId { get; init; }
    public string? PhaseJobKind { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string HangfireJobState { get; init; } = "Unknown";
    public DateTime? CancelRequestedAt { get; init; }
    public bool CanReconcile { get; init; }
    public bool CanCancel { get; init; }
}

public sealed record MonitorChildDto
{
    public Guid ChildId { get; init; }
    public int Sequence { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? AfterKey { get; init; }
    public string? LastKey { get; init; }
    public long RowsRead { get; init; }
    public int AttemptCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastHeartbeatAt { get; init; }
    public string? HangfireJobId { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string HangfireJobState { get; init; } = "Unknown";
    public bool CanRetry { get; init; }
}
