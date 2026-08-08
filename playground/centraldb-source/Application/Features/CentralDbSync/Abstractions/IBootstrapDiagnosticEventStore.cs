namespace Application.Features.CentralDbSync.Abstractions;

using Application.Features.CentralDbSync.Models;

public interface IBootstrapDiagnosticEventStore
{
    Task<long> AppendAsync(BootstrapDiagnosticEvent evt, CancellationToken ct);

    Task<IReadOnlyList<BootstrapDiagnosticEvent>> GetTimelineAsync(
        Guid requestId, int pageIndex, int pageSize, CancellationToken ct);

    Task<long> DeleteBeforeAsync(DateTime cutoffUtc, CancellationToken ct);
}
