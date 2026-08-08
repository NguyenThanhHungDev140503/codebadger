namespace Application.Features.CentralDbSync.Abstractions;

using Application.Features.CentralDbSync.Dtos;

public sealed record BootstrapMonitorTarget
{
    public Guid ParentId { get; init; }
    public Guid? ChildId { get; init; }

    public static BootstrapMonitorTarget Parent(Guid parentId) => new() { ParentId = parentId };
    public static BootstrapMonitorTarget Child(Guid parentId, Guid childId) => new()
    {
        ParentId = parentId,
        ChildId = childId
    };
    public bool IsChild => ChildId.HasValue;
}

public sealed record BootstrapMonitorActionResult
{
    public string Status { get; init; } = string.Empty;
    public string? Message { get; init; }
    public string? HangfireJobId { get; init; }

    public static BootstrapMonitorActionResult Accepted(string? jobId = null, string? message = null) => new()
    {
        Status = "accepted",
        HangfireJobId = jobId,
        Message = message
    };

    public static BootstrapMonitorActionResult NotFound(string message) => new()
    {
        Status = "not_found",
        Message = message
    };

    public static BootstrapMonitorActionResult Conflict(string message) => new()
    {
        Status = "conflict",
        Message = message
    };

    public static BootstrapMonitorActionResult SchedulerFailure(string message) => new()
    {
        Status = "scheduler_failure",
        Message = message
    };
}

public interface IBootstrapMonitorActionService
{
    Task<BootstrapMonitorActionResult> ReconcileAsync(
        BootstrapMonitorTarget target, string initiatedBy, CancellationToken ct);

    Task<BootstrapMonitorActionResult> RetryAsync(
        BootstrapMonitorTarget target, string initiatedBy, CancellationToken ct);

    Task<BootstrapMonitorActionResult> RequestCancelAsync(
        Guid parentId, string initiatedBy, CancellationToken ct);
}
