namespace Application.Features.CentralDbSync.Abstractions;

using Application.Features.CentralDbSync.Dtos;

public interface IBootstrapMonitorQueryService
{
    Task<IReadOnlyList<BootstrapMonitorListItemDto>> GetRequestListAsync(
        string? ruleName, string? status, int pageIndex, int pageSize, CancellationToken ct);

    Task<BootstrapMonitorDetailDto?> GetDetailAsync(Guid requestId, CancellationToken ct);

    Task<IReadOnlyList<BootstrapDiagnosticEventDto>> GetTimelineAsync(
        Guid requestId, int pageIndex, int pageSize, CancellationToken ct);
}
