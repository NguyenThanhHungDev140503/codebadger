using System.Text.Json;
using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Queries;
using Hangfire.Dashboard;

namespace WebApi.Infrastructure.CentralDb;

public sealed class CentralDbSyncMainPageDispatcher : IDashboardDispatcher
{
    public async Task Dispatch(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        httpContext.Response.ContentType = "text/html; charset=utf-8";
        await httpContext.Response.WriteAsync(CentralDbSyncDashboardPage.RenderHtml());
    }
}

public sealed class CentralDbSyncOverviewApiDispatcher : IDashboardDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task Dispatch(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var queryService = httpContext.RequestServices.GetRequiredService<ICentralDbSyncQueryService>();

        var overview = await queryService.GetOverviewAsync(httpContext.RequestAborted);

        httpContext.Response.ContentType = "application/json; charset=utf-8";
        await JsonSerializer.SerializeAsync(httpContext.Response.Body, overview, JsonOptions, httpContext.RequestAborted);
    }
}

public sealed class CentralDbSyncLogsApiDispatcher : IDashboardDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task Dispatch(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var request = httpContext.Request;
        var queryService = httpContext.RequestServices.GetRequiredService<ICentralDbSyncQueryService>();

        var ruleName = request.Query["ruleName"].ToString();
        var outcome = request.Query["outcome"].ToString();
        DateTime? from = DateTime.TryParse(request.Query["from"], out var f) ? DateTime.SpecifyKind(f, DateTimeKind.Utc) : null;
        DateTime? to = DateTime.TryParse(request.Query["to"], out var t) ? DateTime.SpecifyKind(t, DateTimeKind.Utc) : null;
        int pageIndex = int.TryParse(request.Query["pageIndex"], out var p) && p > 0 ? p : 1;
        int pageSize = int.TryParse(request.Query["pageSize"], out var s) && s > 0 ? s : 10;

        var query = new GetSyncLogsQuery
        {
            RuleName = string.IsNullOrWhiteSpace(ruleName) ? null : ruleName,
            Outcome = string.IsNullOrWhiteSpace(outcome) ? null : outcome,
            From = from,
            To = to,
            PageIndex = pageIndex,
            PageSize = pageSize
        };

        var logs = await queryService.GetLogsAsync(query, httpContext.RequestAborted);

        httpContext.Response.ContentType = "application/json; charset=utf-8";
        await JsonSerializer.SerializeAsync(httpContext.Response.Body, logs, JsonOptions, httpContext.RequestAborted);
    }
}

public sealed class CentralDbSyncScheduleApiDispatcher : IDashboardDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task Dispatch(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var scheduleService = httpContext.RequestServices.GetRequiredService<ICentralDbSyncScheduleService>();

        if (HttpMethods.IsGet(httpContext.Request.Method))
        {
            var state = await scheduleService.GetStateAsync();

            httpContext.Response.ContentType = "application/json; charset=utf-8";
            await JsonSerializer.SerializeAsync(
                httpContext.Response.Body,
                new { state },
                JsonOptions,
                httpContext.RequestAborted);
            return;
        }

        if (HttpMethods.IsPost(httpContext.Request.Method))
        {
            ApplyCentralDbSyncScheduleRequest? request;
            CentralDbSyncScheduleAction action;
            string? tier;
            try
            {
                using var reader = new StreamReader(httpContext.Request.Body);
                var body = await reader.ReadToEndAsync();
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                var actionStr = root.GetProperty("action").GetString() ?? string.Empty;
                action = actionStr switch
                {
                    "apply" => CentralDbSyncScheduleAction.Apply,
                    "restoreDefault" or "restore_default" => CentralDbSyncScheduleAction.RestoreDefault,
                    _ => throw new JsonException($"Unknown action '{actionStr}'.")
                };

                tier = root.TryGetProperty("tier", out var tierElement)
                    ? tierElement.GetString()
                    : null;

                request = action == CentralDbSyncScheduleAction.Apply
                    ? JsonSerializer.Deserialize<ApplyCentralDbSyncScheduleRequest>(body, JsonOptions)
                    : null;

                if (action == CentralDbSyncScheduleAction.Apply && request is null)
                    throw new JsonException("Request body is required for apply.");
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException)
            {
                httpContext.Response.StatusCode = 400;
                await WriteError(httpContext, $"Invalid request: {ex.Message}");
                return;
            }

            try
            {
                CentralDbSyncScheduleStateDto state = action switch
                {
                    CentralDbSyncScheduleAction.Apply => await scheduleService.ApplyAsync(request!),
                    CentralDbSyncScheduleAction.RestoreDefault => await scheduleService.RestoreDefaultAsync(tier ?? string.Empty),
                    _ => throw new InvalidOperationException($"Unhandled action '{action}'.")
                };

                httpContext.Response.ContentType = "application/json; charset=utf-8";
                await JsonSerializer.SerializeAsync(
                    httpContext.Response.Body,
                    new { state },
                    JsonOptions,
                    httpContext.RequestAborted);
            }
            catch (ScheduleValidationException ex)
            {
                httpContext.Response.StatusCode = 422;
                await WriteError(httpContext, ex.Message);
            }
            catch (TimeZoneNotFoundException ex)
            {
                httpContext.Response.StatusCode = 422;
                await WriteError(httpContext, ex.Message);
            }

            return;
        }

        httpContext.Response.StatusCode = 405;
    }

    private static async Task WriteError(HttpContext httpContext, string message)
    {
        httpContext.Response.ContentType = "application/json; charset=utf-8";
        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            new { error = message },
            JsonOptions,
            httpContext.RequestAborted);
    }

}

public sealed class BootstrapExplorerListApiDispatcher : IDashboardDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task Dispatch(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var queryService = httpContext.RequestServices.GetRequiredService<IBootstrapMonitorQueryService>();
        var request = httpContext.Request;

        var ruleName = request.Query["ruleName"].ToString();
        var status = request.Query["status"].ToString();
        int pageIndex = int.TryParse(request.Query["pageIndex"], out var p) && p > 0 ? p : 1;
        int pageSize = int.TryParse(request.Query["pageSize"], out var s) && s > 0 && s <= 100 ? s : 20;

        var items = await queryService.GetRequestListAsync(
            string.IsNullOrWhiteSpace(ruleName) ? null : ruleName,
            string.IsNullOrWhiteSpace(status) ? null : status,
            pageIndex, pageSize, httpContext.RequestAborted);

        httpContext.Response.ContentType = "application/json; charset=utf-8";
        await JsonSerializer.SerializeAsync(httpContext.Response.Body, items, JsonOptions, httpContext.RequestAborted);
    }
}

public sealed class BootstrapExplorerDetailApiDispatcher : IDashboardDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task Dispatch(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var queryService = httpContext.RequestServices.GetRequiredService<IBootstrapMonitorQueryService>();

        var requestIdStr = httpContext.Request.Query["requestId"].ToString();
        if (!Guid.TryParse(requestIdStr, out var requestId))
        {
            httpContext.Response.StatusCode = 400;
            return;
        }

        var detail = await queryService.GetDetailAsync(requestId, httpContext.RequestAborted);

        if (detail is null)
        {
            httpContext.Response.StatusCode = 404;
            return;
        }

        httpContext.Response.ContentType = "application/json; charset=utf-8";
        await JsonSerializer.SerializeAsync(httpContext.Response.Body, detail, JsonOptions, httpContext.RequestAborted);
    }
}

public sealed class BootstrapExplorerTimelineApiDispatcher : IDashboardDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task Dispatch(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var queryService = httpContext.RequestServices.GetRequiredService<IBootstrapMonitorQueryService>();
        var request = httpContext.Request;

        var requestIdStr = request.Query["requestId"].ToString();
        if (!Guid.TryParse(requestIdStr, out var requestId))
        {
            httpContext.Response.StatusCode = 400;
            return;
        }

        int pageIndex = int.TryParse(request.Query["pageIndex"], out var p) && p > 0 ? p : 1;
        int pageSize = int.TryParse(request.Query["pageSize"], out var s) && s > 0 && s <= 200 ? s : 50;

        var timeline = await queryService.GetTimelineAsync(requestId, pageIndex, pageSize, httpContext.RequestAborted);

        httpContext.Response.ContentType = "application/json; charset=utf-8";
        await JsonSerializer.SerializeAsync(httpContext.Response.Body, timeline, JsonOptions, httpContext.RequestAborted);
    }
}

public sealed class BootstrapExplorerActionApiDispatcher : IDashboardDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task Dispatch(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var actionService = httpContext.RequestServices.GetRequiredService<IBootstrapMonitorActionService>();

        if (!HttpMethods.IsPost(httpContext.Request.Method))
        {
            httpContext.Response.StatusCode = 405;
            return;
        }

        string initiatedBy = "dashboard";
        Guid parentId = Guid.Empty;
        Guid childId = Guid.Empty;

        try
        {
            using var reader = new StreamReader(httpContext.Request.Body);
            var body = await reader.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(body))
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("parentId", out var pe))
                    Guid.TryParse(pe.GetString(), out parentId);
                if (doc.RootElement.TryGetProperty("childId", out var ce))
                    Guid.TryParse(ce.GetString(), out childId);
            }
        }
        catch
        {
            httpContext.Response.StatusCode = 400;
            return;
        }

        var path = httpContext.Request.Path.Value ?? string.Empty;

        BootstrapMonitorActionResult? result = null;
        if (path.Contains("/parents/reconcile") && parentId != Guid.Empty)
            result = await actionService.ReconcileAsync(BootstrapMonitorTarget.Parent(parentId), initiatedBy, httpContext.RequestAborted);
        else if (path.Contains("/children/retry") && parentId != Guid.Empty && childId != Guid.Empty)
            result = await actionService.RetryAsync(BootstrapMonitorTarget.Child(parentId, childId), initiatedBy, httpContext.RequestAborted);
        else if (path.Contains("/parents/cancel") && parentId != Guid.Empty)
            result = await actionService.RequestCancelAsync(parentId, initiatedBy, httpContext.RequestAborted);

        if (result is null)
        {
            httpContext.Response.StatusCode = 400;
            return;
        }

        var statusCode = result.Status switch
        {
            "accepted" => 202,
            "not_found" => 404,
            "conflict" => 409,
            "scheduler_failure" => 503,
            _ => 500
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json; charset=utf-8";
        await JsonSerializer.SerializeAsync(httpContext.Response.Body, result, JsonOptions, httpContext.RequestAborted);
    }
}
