using Application.Features.CentralDbSync.Models;

namespace Application.Features.CentralDbSync.Abstractions;

/// <summary>
/// Probes the background-job store to classify the Hangfire job that should
/// execute a bootstrap request. Used by the orphan recovery path to distinguish
/// a legitimately-queued job (still alive) from one that is terminal or missing.
/// </summary>
public interface IBootstrapJobStateChecker
{
    /// <summary>
    /// Returns a classified snapshot of the job identified by <paramref name="hangfireJobId"/>.
    /// <c>Alive</c> means the job can still lead to execution.
    /// <c>TerminalFailure</c> means Failed or Deleted.
    /// <c>TerminalSuccess</c> means Succeeded.
    /// <c>Missing</c> means the job record is absent or the id is null/empty.
    /// </summary>
    BootstrapJobStateSnapshot Probe(string? hangfireJobId);
}
