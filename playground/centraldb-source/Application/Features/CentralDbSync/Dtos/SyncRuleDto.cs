namespace Application.Features.CentralDbSync.Dtos;

public sealed record SyncRuleDto(
    string RuleName,
    string TargetTable,
    string SyncMode,
    bool Enabled);
