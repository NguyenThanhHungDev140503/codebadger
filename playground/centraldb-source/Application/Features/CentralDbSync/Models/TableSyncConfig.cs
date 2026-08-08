namespace Application.Features.CentralDbSync.Models;

public sealed record TableSyncConfig
{
    public required string SourceTable { get; init; }
    public string TargetSchema { get; init; } = "report";
    public required string TargetTable { get; init; }
    public string SyncMode { get; init; } = "FullRefresh";
    public string SyncTier { get; init; } = "Hot";
    public string[] Dependency { get; init; } = [];
    public TimeSpan ExpectedSyncInterval { get; init; }
    public TimeSpan MaxAllowedLag { get; init; }
    public string? OwnershipScope { get; init; }
    public bool Enabled { get; init; } = true;
}
