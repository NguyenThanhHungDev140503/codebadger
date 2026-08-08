namespace Application.Features.CentralDbSync.Models;

public enum BootstrapJobStateKind
{
    Alive,
    TerminalFailure,
    TerminalSuccess,
    Missing,
    Unknown
}

public sealed record BootstrapJobStateSnapshot(
    BootstrapJobStateKind Kind,
    string? JobId,
    string? State,
    string? ServerId = null,
    string? ExceptionType = null,
    string? ExceptionMessage = null,
    DateTime? ObservedAt = null);
