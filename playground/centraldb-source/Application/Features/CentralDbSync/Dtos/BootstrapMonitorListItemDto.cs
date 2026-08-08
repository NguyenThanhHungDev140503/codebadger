namespace Application.Features.CentralDbSync.Dtos;

public sealed record BootstrapMonitorListItemDto
{
    public Guid RequestId { get; init; }
    public string RuleName { get; init; } = string.Empty;
    public string RequestStatus { get; init; } = string.Empty;
    public string? BootstrapType { get; init; }
    public int TotalChildren { get; init; }
    public int CompletedChildren { get; init; }
    public int FailedChildren { get; init; }
    public string Health { get; init; } = "Unknown";
    public string? LatestEventType { get; init; }
    public DateTime? LatestEventAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
