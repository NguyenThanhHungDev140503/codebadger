using Application.Common.Models;
using MediatR;

namespace Application.Features.CentralDbSync.Commands;

/// <summary>
/// Triggers the ops/manual batch reconciliation of stale <c>pending_enqueue</c> and
/// <c>queued</c> bootstrap requests. Stale = older than 5 minutes.
/// Queued requests are filtered by Hangfire job liveness per request, so a
/// legitimately-waiting job (including one behind <c>[DisableConcurrentExecution]</c>)
/// is never falsely re-enqueued.
/// </summary>
public sealed record ReconcileBootstrapCommand : IRequest<ApiResponse<object>>;