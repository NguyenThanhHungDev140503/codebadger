using Application.Common.Models;
using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Commands;
using Application.Features.CentralDbSync.Dtos;
using Application.Features.CentralDbSync.Queries;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using WebApi.Abstractions;
using WebApi.Filters;
using WebApi.Infrastructure.Attributes;
using WebApi.Infrastructure.CentralDb;

namespace WebApi.Controllers.CentralDb;

[ApiAuthorize]
[Route("api/central-db-sync")]
[RequireFeature(Feature.CentralDbSync)]
public sealed class CentralDbSyncController(
    IMediator mediator,
    ICentralDbSyncScheduleService scheduleService)
    : BaseApiController<CentralDbSyncController>(mediator)
{
    /// <summary>
    /// Returns the checkpoint freshness / sync lag status for a table.
    /// ResponseCode 200 (Healthy/Degraded) · ResponseCode 409 (No checkpoint yet)
    /// </summary>
    /// <param name="ruleName">Registered Central DB Sync rule name, for example <c>ERP.PurchaseOrders</c>.</param>
    /// <param name="maxAllowedLagMinutes">Optional positive integer freshness threshold in minutes. If omitted, the rule tier default is used.</param>
    [HttpGet("{ruleName}/status")]
    [ProducesResponseType(typeof(ApiResponse<SyncStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSyncStatus(
        string ruleName,
        [FromQuery] int? maxAllowedLagMinutes = null)
    {
        return Ok(await Mediator.Send(new GetSyncStatusQuery(ruleName, maxAllowedLagMinutes)));
    }

    /// <summary>
    /// Returns the current Hangfire recurring schedules for Hot and Cold sync tiers.
    /// </summary>
    [HttpGet("schedule")]
    [ProducesResponseType(typeof(ApiResponse<CentralDbSyncScheduleStateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchedule()
    {
        var state = await scheduleService.GetStateAsync();
        return Ok(ApiResponse<CentralDbSyncScheduleStateDto>.Success(state));
    }

    /// <summary>
    /// Applies a temporary Hangfire recurring schedule for one sync tier.
    /// The change lives in Hangfire storage and may be reset by application restart/deployment.
    /// </summary>
    /// <param name="request">JSON body: <c>Tier</c> must be <c>Hot</c> or <c>Cold</c>; <c>CronExpression</c> is a five-field cron expression; <c>TimeZoneKey</c> is a supported timezone key such as <c>vietnam</c>.</param>
    [HttpPut("schedule")]
    [ProducesResponseType(typeof(ApiResponse<CentralDbSyncScheduleStateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApplySchedule([FromBody] ApplyCentralDbSyncScheduleRequest? request)
    {
        if (request is null)
        {
            return Ok(ApiResponse<CentralDbSyncScheduleStateDto>.Failure(
                "Request body is required.",
                StatusCodes.Status400BadRequest));
        }

        try
        {
            var state = await scheduleService.ApplyAsync(request);
            return Ok(ApiResponse<CentralDbSyncScheduleStateDto>.Success(state));
        }
        catch (ScheduleValidationException ex)
        {
            return Ok(ApiResponse<CentralDbSyncScheduleStateDto>.Failure(
                ex.Message,
                StatusCodes.Status422UnprocessableEntity));
        }
        catch (TimeZoneNotFoundException ex)
        {
            return Ok(ApiResponse<CentralDbSyncScheduleStateDto>.Failure(
                ex.Message,
                StatusCodes.Status422UnprocessableEntity));
        }
    }

    /// <summary>
    /// Restores the configured default Hangfire recurring schedule for one sync tier.
    /// </summary>
    /// <param name="request">JSON body with <c>Tier</c>, which must be <c>Hot</c> or <c>Cold</c>.</param>
    [HttpPost("schedule/restore-default")]
    [ProducesResponseType(typeof(ApiResponse<CentralDbSyncScheduleStateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RestoreDefaultSchedule(
        [FromBody] RestoreCentralDbSyncScheduleRequest? request,
        CancellationToken ct = default)
    {
        if (request is null)
        {
            return Ok(ApiResponse<CentralDbSyncScheduleStateDto>.Failure(
                "Request body is required.",
                StatusCodes.Status400BadRequest));
        }

        try
        {
            var state = await scheduleService.RestoreDefaultAsync(request.Tier);
            return Ok(ApiResponse<CentralDbSyncScheduleStateDto>.Success(state));
        }
        catch (ScheduleValidationException ex)
        {
            return Ok(ApiResponse<CentralDbSyncScheduleStateDto>.Failure(
                ex.Message,
                StatusCodes.Status422UnprocessableEntity));
        }
    }

    /// <summary>
    /// Starts an asynchronous bootstrap for a registered sync rule.
    /// The request is processed in the background and can be tracked by its request ID.
    /// </summary>
    /// <param name="ruleName">Registered rule name to bootstrap.</param>
    [HttpPost("bootstrap/{ruleName}")]
    [ProducesResponseType(typeof(ApiResponse<BootstrapResponseDto>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<BootstrapResponseDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> TriggerBootstrap(string ruleName)
    {
        var result = await Mediator.Send(new TriggerBootstrapCommand(ruleName));

        // StatusUrl needs IUrlHelper, which is a WebApi concern — the handler cannot
        // build it. Mutate Data in place so Message and Errors survive.
        if (result is { Successed: true, Data: not null })
        {
            result.Data = result.Data with
            {
                StatusUrl = Url.Action(nameof(GetBootstrapStatus),
                    null, new { requestId = result.Data.RequestId },
                    Request.Scheme)
            };
        }

        return result.ResponseCode == StatusCodes.Status409Conflict
            ? Conflict(result)
            : Accepted(result);
    }

    /// <summary>
    /// Triggers the ops batch reconciler for stale <c>pending_enqueue</c> and
    /// <c>queued</c> bootstrap requests. Intended for manual recovery after a
    /// deploy or process-recycle orphaned the primary Hangfire jobs.
    /// </summary>
    /// <remarks>Scans stale bootstrap requests and schedules recovery where the original Hangfire job is no longer alive.</remarks>
    [HttpPost("bootstrap/reconcile")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReconcileBootstrap(CancellationToken ct = default)
    {
        return Ok(await Mediator.Send(new ReconcileBootstrapCommand(), ct));
    }

    /// <summary>
    /// Returns the current state of a bootstrap request.
    /// ResponseCode 200 with request state · ResponseCode 404 Unknown
    /// </summary>
    /// <param name="requestId">Bootstrap request identifier in standard GUID format.</param>
    [HttpGet("bootstrap/{requestId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BootstrapStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBootstrapStatus(Guid requestId)
    {
        return Ok(await Mediator.Send(
            new GetBootstrapStatusQuery(requestId)));
    }

    /// <summary>
    /// Returns paginated bootstrap requests for the Bootstrap Jobs screen.
    /// </summary>
    /// <param name="query">Optional <c>RuleName</c>/<c>Status</c> filters and pagination. <c>PageIndex</c> is 1-based; <c>PageSize</c> is 1-400; <c>SortOrder</c> is asc or desc.</param>
    [HttpGet("bootstrap")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<BootstrapJobListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBootstrapJobs(
        [FromQuery] GetBootstrapJobsQuery query,
        CancellationToken ct = default)
    {
        return Ok(await Mediator.Send(query, ct));
    }

    /// <summary>
    /// Returns paginated sync audit logs filtered by ruleName, outcome, and date range.
    /// </summary>
    /// <param name="query">Optional <c>RuleName</c>, <c>Outcome</c>, <c>From</c> and <c>To</c> filters plus pagination. Date values should use <c>yyyy-MM-dd</c>.</param>
    [HttpGet("logs")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<SyncRunLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSyncLogs(
        [FromQuery] GetSyncLogsQuery query,
        CancellationToken ct = default)
    {
        return Ok(await Mediator.Send(query, ct));
    }

    /// <summary>
    /// Returns table sync status and dashboard summary for all registered rules.
    /// </summary>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(ApiResponse<SyncOverviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSyncOverview(CancellationToken ct = default)
    {
        return Ok(await Mediator.Send(new GetSyncOverviewQuery(), ct));
    }

    /// <summary>
    /// Returns aggregated sync history points for Monitoring charts.
    /// </summary>
    /// <param name="from">Optional inclusive start date/time in ISO-8601 format; date-only values may use <c>yyyy-MM-dd</c>.</param>
    /// <param name="to">Optional inclusive end date/time in ISO-8601 format; date-only values may use <c>yyyy-MM-dd</c>.</param>
    /// <param name="bucketMinutes">Aggregation bucket size in minutes. Defaults to 60; must be positive.</param>
    [HttpGet("monitoring/history")]
    [ProducesResponseType(typeof(ApiResponse<List<MonitoringHistoryPointDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMonitoringHistory(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int bucketMinutes = 60,
        CancellationToken ct = default)
    {
        return Ok(await Mediator.Send(
            new GetMonitoringHistoryQuery(from, to, bucketMinutes), ct));
    }

    /// <summary>
    /// Returns aggregate sync statistics for Monitoring cards.
    /// </summary>
    /// <param name="from">Optional inclusive start date/time in ISO-8601 format; date-only values may use <c>yyyy-MM-dd</c>.</param>
    /// <param name="to">Optional inclusive end date/time in ISO-8601 format; date-only values may use <c>yyyy-MM-dd</c>.</param>
    [HttpGet("monitoring/stats")]
    [ProducesResponseType(typeof(ApiResponse<MonitoringStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMonitoringStats(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct = default)
    {
        return Ok(await Mediator.Send(
            new GetMonitoringStatsQuery(from, to), ct));
    }

    /// <summary>
    /// Returns all registered sync rules with their sync config enabled flag.
    /// </summary>
    /// <param name="query">Pagination query. <c>PageIndex</c> is 1-based; <c>PageSize</c> is 1-400; <c>SortOrder</c> is asc or desc.</param>
    [HttpGet("rules")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<SyncRuleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRules(
        [FromQuery] GetSyncRulesQuery query,
        CancellationToken ct = default)
    {
        return Ok(await Mediator.Send(query, ct));
    }

    /// <summary>
    /// Enables or disables recurring sync for a rule.
    /// </summary>
    /// <param name="ruleName">Registered Central DB Sync rule name.</param>
    /// <param name="body">JSON body with boolean <c>Enabled</c>: true enables recurring sync, false disables it.</param>
    [HttpPatch("{ruleName}/enabled")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetEnabled(
        string ruleName,
        [FromBody] SetEnabledRequest body,
        CancellationToken ct = default)
    {
        return Ok(await Mediator.Send(
            new SetEnabledCommand(ruleName, body.Enabled), ct));
    }

    /// <summary>
    /// Checks whether Change Tracking is enabled at the SQL Server level.
    /// Queries sys.change_tracking_tables directly — independent of app config.
    /// </summary>
    /// <param name="ruleName">Registered rule name whose source SQL Server table should be checked.</param>
    [HttpGet("{ruleName}/ct-status")]
    [ProducesResponseType(typeof(ApiResponse<CtStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCtStatus(
        string ruleName,
        CancellationToken ct = default)
    {
        return Ok(await Mediator.Send(
            new GetCtStatusQuery(ruleName), ct));
    }

    /// <summary>
    /// Returns a filterable, paginated list of Bootstrap Monitor requests.
    /// </summary>
    /// <param name="query">Optional <c>RuleName</c>/<c>Status</c> filters and pagination.</param>
    [HttpGet("bootstrap-monitor")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<BootstrapMonitorListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<BootstrapMonitorListItemDto>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBootstrapMonitorList(
        [FromQuery] GetBootstrapMonitorListQuery query,
        CancellationToken ct = default)
    {
        return Ok(await Mediator.Send(query, ct));
    }

    /// <summary>
    /// Returns the full detail of a Bootstrap Monitor request including
    /// parent, ordered children, and diagnostic timeline.
    /// </summary>
    /// <param name="requestId">Bootstrap request identifier.</param>
    [HttpGet("bootstrap-monitor/{requestId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BootstrapMonitorDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BootstrapMonitorDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBootstrapMonitorDetail(
        Guid requestId,
        CancellationToken ct = default)
    {
        return Ok(await Mediator.Send(
            new GetBootstrapMonitorDetailQuery(requestId), ct));
    }

    /// <summary>
    /// Returns the chronological diagnostic event timeline for a bootstrap request.
    /// </summary>
    /// <param name="requestId">Bootstrap request identifier.</param>
    /// <param name="pageIndex">1-based page number. Defaults to 1.</param>
    /// <param name="pageSize">Events per page (1-200). Defaults to 50.</param>
    [HttpGet("bootstrap-monitor/{requestId:guid}/timeline")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BootstrapDiagnosticEventDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BootstrapDiagnosticEventDto>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBootstrapDiagnosticTimeline(
        Guid requestId,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        return Ok(await Mediator.Send(
            new GetBootstrapDiagnosticTimelineQuery(requestId, pageIndex, pageSize), ct));
    }

    /// <summary>
    /// Reconciles a bootstrap parent that has lost or stale Hangfire ownership.
    /// </summary>
    /// <param name="parentId">Bootstrap parent identifier.</param>
    [HttpPost("bootstrap-monitor/parents/{parentId:guid}/reconcile")]
    [ProducesResponseType(typeof(ApiResponse<BootstrapMonitorActionResult>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<BootstrapMonitorActionResult>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BootstrapMonitorActionResult>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<BootstrapMonitorActionResult>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ReconcileBootstrapParent(
        Guid parentId,
        CancellationToken ct = default)
    {
        return Ok(await Mediator.Send(
            new ReconcileBootstrapParentCommand(parentId), ct));
    }

    /// <summary>
    /// Cooperatively cancels an active bootstrap parent. The parent transitions
    /// to <c>cancel_requested</c>; workers stop at the next safe checkpoint.
    /// </summary>
    /// <param name="parentId">Bootstrap parent identifier.</param>
    [HttpPost("bootstrap-monitor/parents/{parentId:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<BootstrapMonitorActionResult>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<BootstrapMonitorActionResult>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BootstrapMonitorActionResult>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<BootstrapMonitorActionResult>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CancelBootstrapParent(
        Guid parentId,
        CancellationToken ct = default)
    {
        return Ok(await Mediator.Send(
            new CancelBootstrapParentCommand(parentId), ct));
    }

    /// <summary>
    /// Retries a failed or recovery-exhausted bootstrap child.
    /// </summary>
    /// <param name="parentId">Bootstrap parent identifier.</param>
    /// <param name="childId">Bootstrap child identifier.</param>
    [HttpPost("bootstrap-monitor/parents/{parentId:guid}/children/{childId:guid}/retry")]
    [ProducesResponseType(typeof(ApiResponse<BootstrapMonitorActionResult>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<BootstrapMonitorActionResult>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BootstrapMonitorActionResult>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<BootstrapMonitorActionResult>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> RetryBootstrapChild(
        Guid parentId,
        Guid childId,
        CancellationToken ct = default)
    {
        return Ok(await Mediator.Send(
            new RetryBootstrapChildCommand(parentId, childId), ct));
    }
}

/// <summary>
/// Request body for PATCH {ruleName}/enabled.
/// </summary>
public sealed record SetEnabledRequest(
    /// <param name="Enabled">Whether recurring synchronization is enabled for the rule.</param>
    bool Enabled);

/// <summary>
/// Request body for POST schedule/restore-default.
/// </summary>
public sealed record RestoreCentralDbSyncScheduleRequest(
    /// <param name="Tier">Sync tier to restore: <c>Hot</c> or <c>Cold</c>.</param>
    string Tier);
