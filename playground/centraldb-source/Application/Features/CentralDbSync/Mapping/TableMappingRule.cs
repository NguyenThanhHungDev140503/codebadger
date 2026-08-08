using Application.Features.CentralDbSync.Models;

namespace Application.Features.CentralDbSync.Mapping;

public sealed record TableMappingRule
{
    public required string RuleName { get; init; }
    public required SourceSpec Source { get; init; }
    public required TargetSpec Target { get; init; }
    public required IReadOnlyList<ColumnMapping> Columns { get; init; }

    /// <summary>
    /// Hangfire scheduling tier for this rule. Hot and Cold run in separate recurring jobs.
    /// </summary>
    public string SyncTier { get; init; } = "Cold";
    public string[] Dependency { get; init; } = [];
    public TimeSpan ExpectedSyncInterval { get; init; } = TimeSpan.FromHours(1);
    public TimeSpan MaxAllowedLag { get; init; } = TimeSpan.FromHours(2);
    public string OwnershipScope { get; init; } = "erp";
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// When <c>true</c>, this rule uses the scalable parent-child bootstrap flow
    /// with dynamic staging and sequential child jobs. When <c>false</c> (the default),
    /// the current in-memory bootstrap flow runs unchanged.
    /// </summary>
    public bool UseScalableBootstrap { get; init; }

    public TableSyncConfig ToTableSyncConfig() => new()
    {
        SourceTable = RuleName,
        TargetSchema = Target.Schema,
        TargetTable = Target.Table,
        SyncMode = "ChangeTracking",
        SyncTier = SyncTier,
        Dependency = Dependency,
        ExpectedSyncInterval = ExpectedSyncInterval,
        MaxAllowedLag = MaxAllowedLag,
        OwnershipScope = OwnershipScope,
        Enabled = Enabled
    };
}
