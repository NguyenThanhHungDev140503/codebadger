namespace Application.Features.CentralDbSync.Dtos;

public sealed record MonitoringHistoryPointDto(
    DateTime Timestamp,
    long? SyncLagMs,
    int RowsUpserted,
    int SuccessCount,
    int FailureCount);
