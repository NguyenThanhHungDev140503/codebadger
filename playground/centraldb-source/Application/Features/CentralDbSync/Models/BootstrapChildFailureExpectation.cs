namespace Application.Features.CentralDbSync.Models;

/// <summary>Durable child snapshot guarded when terminalizing a scalable bootstrap.</summary>
public sealed record BootstrapChildFailureExpectation(
    Guid ChildId,
    Guid ParentId,
    string ExpectedStatus,
    string? ExpectedJobId);
