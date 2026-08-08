namespace Application.Features.CentralDbSync.Dtos;

public sealed record SyncOverviewDto(
    List<TableSyncOverviewDto> Items,
    int RunningBootstrapJobs,
    int ErrorsLast24h);
