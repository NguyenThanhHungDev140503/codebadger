using Application.Common.Models;
using Application.Features.CentralDbSync.Dtos;
using Application.Features.CentralDbSync.Queries;

namespace Application.Features.CentralDbSync.Abstractions;

public interface ICentralDbSyncQueryService
{
    Task<PaginatedResponse<BootstrapJobListItemDto>> GetBootstrapJobsAsync(
        GetBootstrapJobsQuery query, CancellationToken ct);

    Task<PaginatedResponse<SyncRunLogDto>> GetLogsAsync(
        GetSyncLogsQuery query, CancellationToken ct);

    Task<SyncOverviewDto> GetOverviewAsync(
        CancellationToken ct);

    Task<IReadOnlyList<MonitoringHistoryPointDto>> GetMonitoringHistoryAsync(
        GetMonitoringHistoryQuery query, CancellationToken ct);

    Task<MonitoringStatsDto> GetMonitoringStatsAsync(
        GetMonitoringStatsQuery query, CancellationToken ct);
}
