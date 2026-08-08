using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Models;
using Microsoft.Extensions.Logging;

namespace Application.Features.CentralDbSync.Services;

/// <summary>
/// Terminates a scalable bootstrap across both records that hold per-rule ownership.
/// The parent row is guarded by a partial unique index over the active statuses and the
/// linked bootstrap_request is guarded by its own active-status check, so a failure that
/// leaves either one active blocks every later bootstrap of that rule until an operator
/// clears it by hand.
/// </summary>
public sealed class BootstrapFailureService(
    IBootstrapParentStore parentStore,
    IBootstrapRequestStore requestStore,
    IBootstrapDiagnosticEventStore eventStore,
    ILogger<BootstrapFailureService> logger)
{
    public async Task<bool> FailAsync(
        BootstrapParent parent,
        BootstrapChildFailureExpectation? childExpectation,
        string errorCode,
        string errorMessage,
        CancellationToken ct)
    {
        errorMessage = BootstrapDiagnosticSanitizer.Sanitize(errorMessage)
            ?? "Bootstrap failure details were unavailable.";
        if (!parent.BootstrapRequestId.HasValue)
        {
            return await parentStore.TryFailAsync(parent.ParentId, parent.FencingToken,
                errorCode, errorMessage, ct);
        }

        var request = await requestStore.GetAsync(parent.BootstrapRequestId.Value, ct);
        if (request is null)
            return false;

        var requestExpectation = new BootstrapRecoveryExpectation(request.RequestId, request.Status,
            request.HangfireJobId ?? string.Empty, request.ReconcileAttemptCount);
        var terminalized = childExpectation is null
            ? await requestStore.TryFailScalableAsync(requestExpectation, parent.ParentId,
                parent.FencingToken, parent.Status, parent.LastHeartbeatAt, parent.PhaseJobId,
                errorCode, errorMessage, ct)
            : await requestStore.TryFailScalableChildAsync(requestExpectation, childExpectation,
                parent.FencingToken, parent.Status, parent.LastHeartbeatAt, parent.PhaseJobId,
                errorCode, errorMessage, ct);

        if (terminalized)
        {
            await eventStore.AppendAsync(BootstrapDiagnosticEvent.Create(
                parent.BootstrapRequestId ?? Guid.Empty, parent.ParentId, childExpectation?.ChildId,
                BootstrapDiagnosticEntityType.Parent, BootstrapDiagnosticEventType.Failed,
                parent.Status, BootstrapParentStatus.Failed,
                null, parent.FencingToken.ToString(), null, null,
                errorCode, errorMessage, "system"), ct);

            logger.LogError("Scalable bootstrap terminalized for parent {ParentId}, request {RequestId}: {ErrorCode}",
                parent.ParentId, request.RequestId, errorCode);
        }
        else
            logger.LogDebug("Scalable bootstrap terminalization CAS lost for parent {ParentId}, request {RequestId}",
                parent.ParentId, request.RequestId);

        return terminalized;
    }
}
