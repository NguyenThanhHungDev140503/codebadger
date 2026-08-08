namespace Application.Features.CentralDbSync.Dtos;

public sealed record SyncStatusDto(
    string SourceTable,
    string Status,
    long? LastSyncVersion,
    DateTime? LastSuccessAt,
    int ConsecutiveFailureCount,
    string SyncStatus);
