namespace Application.Features.CentralDbSync.Dtos;

public sealed record SyncRunLogDto(
    long Id,
    string RuleName,
    Guid RunId,
    string Mode,
    string Outcome,
    int RowsRead,
    int RowsUpserted,
    int RowsDeactivated,
    int RowsDeleted,
    long? CheckpointBefore,
    long? CheckpointAfter,
    DateTime StartedAt,
    DateTime? FinishedAt,
    long? DurationMs,
    string? ErrorCode,
    string? ErrorMessage);
