namespace Infrastructure.CentralDbSync;

using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Models;
using Hangfire;
using Hangfire.Storage;
using System.Collections.Generic;

/// <summary>
/// Hangfire-backed implementation of <see cref="IBootstrapJobStateChecker"/>.
/// Mirrors the established pattern in <c>HangfireCentralDbSyncRecurringJobReader</c>
/// of reaching <c>JobStorage.Current</c> directly from the infrastructure layer.
/// </summary>
public sealed class HangfireBootstrapJobStateChecker : IBootstrapJobStateChecker
{
    public BootstrapJobStateSnapshot Probe(string? hangfireJobId)
    {
        if (string.IsNullOrWhiteSpace(hangfireJobId))
            return Classify(hangfireJobId, null, null);

        using var connection = JobStorage.Current.GetConnection();
        var jobData = connection.GetJobData(hangfireJobId);
        if (jobData is null)
            return Classify(hangfireJobId, null, null);

        var stateData = string.Equals(jobData.State, "Failed", StringComparison.Ordinal)
            ? connection.GetStateData(hangfireJobId)
            : null;
        return Classify(hangfireJobId, jobData.State, stateData?.Data);
    }

    public static BootstrapJobStateSnapshot Classify(
        string? jobId, string? state, IDictionary<string, string>? stateData)
    {
        var kind = state switch
        {
            "Enqueued" or "Scheduled" or "Processing" or "Awaiting" => BootstrapJobStateKind.Alive,
            "Succeeded" => BootstrapJobStateKind.TerminalSuccess,
            "Failed" or "Deleted" => BootstrapJobStateKind.TerminalFailure,
            null or "" => BootstrapJobStateKind.Missing,
            _ => BootstrapJobStateKind.Unknown
        };

        string? serverId = null;
        string? exceptionType = null;
        string? exceptionMessage = null;
        stateData?.TryGetValue("ServerId", out serverId);
        stateData?.TryGetValue("ExceptionType", out exceptionType);
        stateData?.TryGetValue("ExceptionMessage", out exceptionMessage);
        return new(kind, jobId, state, Sanitize(serverId), Sanitize(exceptionType), Sanitize(exceptionMessage), DateTime.UtcNow);
    }

    /// Delegates to the shared <see cref="BootstrapDiagnosticSanitizer"/>.
    public static string? Sanitize(string? value)
        => BootstrapDiagnosticSanitizer.Sanitize(value);
}
