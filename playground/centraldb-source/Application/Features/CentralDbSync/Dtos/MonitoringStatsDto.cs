namespace Application.Features.CentralDbSync.Dtos;

public sealed record MonitoringStatsDto(
    decimal SuccessRate,
    decimal FailureRate,
    long? AvgLagTimeMs);
