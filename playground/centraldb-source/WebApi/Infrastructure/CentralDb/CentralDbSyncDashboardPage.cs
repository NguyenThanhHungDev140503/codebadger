namespace WebApi.Infrastructure.CentralDb;

public static class CentralDbSyncDashboardPage
{
    public static string RenderHtml()
    {
        return """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <title>Central DB Sync Monitor</title>
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@3.4.1/dist/css/bootstrap.min.css" />
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif; padding-top: 20px; background-color: #f8f9fa; }
        .sync-container { max-width: 1400px; margin: 0 auto; padding: 0 15px; }
        .card { background: #fff; border-radius: 4px; border: 1px solid #e1e4e8; padding: 20px; margin-bottom: 20px; box-shadow: 0 1px 3px rgba(0,0,0,0.05); }
        .badge-healthy { background-color: #28a745; }
        .badge-degraded { background-color: #ffc107; color: #212529; }
        .badge-failed { background-color: #dc3545; }
        .badge-neversynced { background-color: #17a2b8; }
        .badge-disabled { background-color: #6c757d; }
        .badge-succeeded { background-color: #28a745; }
        .badge-no_changes { background-color: #6c757d; }
        .filter-panel { background: #f1f3f5; padding: 15px; border-radius: 4px; margin-bottom: 15px; }
        .modal-body pre { background: #272822; color: #f8f8f2; padding: 12px; border-radius: 4px; word-break: break-all; white-space: pre-wrap; }
        .dashboard-back { margin-bottom: 10px; }
        .nav-tabs { margin-bottom: 20px; }
        .table-responsive { overflow-x: auto; }
        .col-no { width: 64px; text-align: center; }
        .sync-pagination { display: flex; align-items: center; flex-wrap: wrap; gap: 10px; margin-top: 15px; color: #333; font-size: 14px; }
        .sync-page-list { display: inline-flex; align-items: center; border: 1px solid #ddd; border-radius: 4px; overflow: hidden; background: #fff; }
        .sync-page-btn { min-width: 38px; height: 34px; padding: 0 10px; border: 0; border-right: 1px solid #ddd; background: #fff; color: #337ab7; font-weight: 400; line-height: 34px; }
        .sync-page-btn:last-child { border-right: 0; }
        .sync-page-btn:hover:not(:disabled):not(.active) { background: #eee; color: #23527c; }
        .sync-page-btn.active { background: #337ab7; color: #fff; font-weight: 600; }
        .sync-page-btn:disabled { color: #aaa; cursor: not-allowed; background: #f9f9f9; }
        .sync-page-ellipsis { min-width: 38px; height: 34px; display: inline-flex; align-items: center; justify-content: center; border-right: 1px solid #ddd; color: #555; background: #fff; }
        .sync-page-summary { color: #555; white-space: nowrap; }
        .sync-page-size { display: inline-flex; align-items: center; gap: 8px; color: #555; font-weight: 600; }
        .sync-page-size select { width: 110px; height: 34px; border-radius: 4px; font-weight: 400; }
    </style>
</head>
<body>
<div class="sync-container">
    <div class="dashboard-back">
        <a class="btn btn-default btn-sm" href="/hangfire">
            <span class="glyphicon glyphicon-chevron-left"></span> Back to Hangfire Dashboard
        </a>
    </div>
    <div class="page-header clearfix">
        <h2 class="pull-left" style="margin-top: 0;">Central DB Sync Monitoring Dashboard</h2>
        <button id="btnRefresh" class="btn btn-default pull-right"><span class="glyphicon glyphicon-refresh"></span> Refresh</button>
    </div>

    <ul class="nav nav-tabs" id="mainTabs">
        <li class="active"><a href="#overviewTab" data-toggle="tab">Table Health Overview</a></li>
        <li><a href="#logsTab" data-toggle="tab">Sync Log Audit Trail</a></li>
        <li><a href="#scheduleTab" data-toggle="tab">Schedule</a></li>
        <li><a href="#bootstrapTab" data-toggle="tab">Bootstrap Explorer</a></li>
    </ul>

    <div class="tab-content">
        <!-- TAB 1: OVERVIEW -->
        <div class="tab-pane active" id="overviewTab">
            <div class="card">
                <h4>Table Replication Status</h4>
                <div class="table-responsive">
                    <table class="table table-striped table-hover" id="overviewTable">
                        <thead>
                            <tr>
                                <th class="col-no">No.</th>
                                <th>Rule Name</th>
                                <th>Sync Tier</th>
                                <th>Status</th>
                                <th>Health</th>
                                <th>Sync Lag</th>
                                <th>Last Success</th>
                                <th>Failures</th>
                                <th>Latest Run</th>
                                <th>Upserted</th>
                                <th>Duration</th>
                                <th>Last Error</th>
                            </tr>
                        </thead>
                        <tbody id="overviewTbody">
                            <tr><td colspan="12" class="text-center">Loading overview...</td></tr>
                        </tbody>
                    </table>
                </div>
                <div class="sync-pagination" id="overviewPagination"></div>
            </div>
        </div>

        <!-- TAB 2: AUDIT LOGS -->
        <div class="tab-pane" id="logsTab">
            <div class="card">
                <div class="filter-panel form-inline">
                    <div class="form-group" style="margin-right: 10px;">
                        <label for="filterRule">Rule Name: </label>
                        <input type="text" id="filterRule" class="form-control input-sm" placeholder="e.g. CRM.Partners" />
                    </div>
                    <div class="form-group" style="margin-right: 10px;">
                        <label for="filterOutcome">Outcome: </label>
                        <select id="filterOutcome" class="form-control input-sm">
                            <option value="">-- All Outcomes --</option>
                            <option value="succeeded">succeeded</option>
                            <option value="no_changes">no_changes</option>
                            <option value="failed">failed</option>
                            <option value="skipped_locked">skipped_locked</option>
                            <option value="skipped_dependency">skipped_dependency</option>
                            <option value="requires_full_resync">requires_full_resync</option>
                        </select>
                    </div>
                    <div class="form-group" style="margin-right: 10px;">
                        <label for="filterFrom">From (UTC): </label>
                        <input type="datetime-local" id="filterFrom" class="form-control input-sm" />
                    </div>
                    <div class="form-group" style="margin-right: 10px;">
                        <label for="filterTo">To (UTC): </label>
                        <input type="datetime-local" id="filterTo" class="form-control input-sm" />
                    </div>
                    <button id="btnFilter" class="btn btn-primary btn-sm">Filter</button>
                    <button id="btnReset" class="btn btn-default btn-sm">Reset</button>
                </div>

                <div class="table-responsive">
                    <table class="table table-striped table-hover" id="logsTable">
                        <thead>
                            <tr>
                                <th class="col-no">No.</th>
                                <th>Started At (UTC)</th>
                                <th>Rule Name</th>
                                <th>Mode</th>
                                <th>Outcome</th>
                                <th>Read</th>
                                <th>Upserted</th>
                                <th>Deact/Del</th>
                                <th>Checkpoints</th>
                                <th>Duration</th>
                                <th>Error Detail</th>
                            </tr>
                        </thead>
                        <tbody id="logsTbody">
                            <tr><td colspan="11" class="text-center">Select or query logs...</td></tr>
                        </tbody>
                    </table>
                </div>
                <div class="sync-pagination" id="logsPagination"></div>
            </div>
        </div>

        <!-- TAB 3: SCHEDULE -->
        <div class="tab-pane" id="scheduleTab">
            <div class="card">
                <h4>Sync Schedule</h4>
                <div class="alert alert-warning">
                    <strong>Temporary change</strong> &mdash; Resets after application restart or deployment.
                </div>

                <div class="form-group">
                    <label for="scheduleTier">Sync tier:</label>
                    <select id="scheduleTier" class="form-control input-sm" style="width: 160px; display: inline-block; margin-left: 5px;">
                        <option value="Hot" selected>Hot</option>
                        <option value="Cold">Cold</option>
                    </select>
                </div>

                <div id="scheduleCurrentState">
                    <p><strong>Recurring job:</strong> <span id="currentRecurringJob">-</span></p>
                    <p><strong>Current CRON:</strong> <span id="currentCron">-</span></p>
                    <p><strong>Timezone:</strong> <span id="currentTimezone">-</span></p>
                    <p><strong>Next execution (UTC):</strong> <span id="nextExecution">-</span></p>
                    <p><strong>Default CRON:</strong> <span id="defaultCron">-</span></p>
                </div>

                <hr />

                <div class="radio">
                    <label><input type="radio" name="scheduleMode" id="modeBuilder" value="builder" checked /> Builder</label>
                    <label style="margin-left: 20px;"><input type="radio" name="scheduleMode" id="modeAdvanced" value="advanced" /> Advanced CRON</label>
                </div>

                <!-- Builder mode -->
                <div id="scheduleBuilderFields">
                    <div class="form-group">
                        <label for="scheduleFrequency">Frequency:</label>
                        <select id="scheduleFrequency" class="form-control input-sm" style="width: 200px; display: inline-block; margin-left: 5px;">
                            <option value="custom" selected>Custom (5-field CRON)</option>
                            <option value="minutes">Minutes</option>
                            <option value="hourly">Hourly</option>
                            <option value="daily">Daily</option>
                            <option value="weekly">Weekly</option>
                            <option value="monthly">Monthly</option>
                        </select>
                    </div>

                    <!-- custom: 5-field CRON -->
                    <div id="bldCustomFields">
                        <div class="form-inline">
                            <div class="form-group" style="margin-right: 5px;">
                                <label>Minute (0-59):</label>
                                <input type="number" id="bldMinute" class="form-control input-sm" min="0" max="59" value="*" style="width: 70px;" />
                            </div>
                            <div class="form-group" style="margin-right: 5px;">
                                <label>Hour (0-23):</label>
                                <input type="number" id="bldHour" class="form-control input-sm" min="0" max="23" value="*" style="width: 70px;" />
                            </div>
                            <div class="form-group" style="margin-right: 5px;">
                                <label>Day of Month (1-31):</label>
                                <input type="number" id="bldDayOfMonth" class="form-control input-sm" min="1" max="31" value="*" style="width: 70px;" />
                            </div>
                            <div class="form-group" style="margin-right: 5px;">
                                <label>Month (1-12):</label>
                                <input type="number" id="bldMonth" class="form-control input-sm" min="1" max="12" value="*" style="width: 60px;" />
                            </div>
                            <div class="form-group" style="margin-right: 5px;">
                                <label>Day of Week (0-7):</label>
                                <input type="number" id="bldDayOfWeek" class="form-control input-sm" min="0" max="7" value="*" style="width: 60px;" />
                            </div>
                        </div>
                        <div id="bldDayWarning" class="text-warning" style="display: none; margin-top: 5px;">
                            <span class="glyphicon glyphicon-warning-sign"></span>
                            Months without day 29, 30, or 31 will have no scheduled run for this expression.
                        </div>
                    </div>

                    <!-- minutes: every N minutes -->
                    <div id="bldMinutesFields" style="display: none;">
                        <div class="form-inline">
                            <div class="form-group">
                                <label>Run every</label>
                                <input type="number" id="bldEveryNInterval" class="form-control input-sm" min="1" value="10" style="width: 60px; margin: 0 5px;" />
                                <span>minute(s)</span>
                            </div>
                        </div>
                    </div>

                    <!-- hourly: minute past the hour -->
                    <div id="bldHourlyFields" style="display: none;">
                        <div class="form-inline">
                            <div class="form-group">
                                <label>At minute</label>
                                <select id="bldEachHourMinute" class="form-control input-sm" style="width: 90px; margin: 0 5px;">
                                    <option value="0">00</option>
                                    <option value="5">05</option>
                                    <option value="10">10</option>
                                    <option value="15" selected>15</option>
                                    <option value="20">20</option>
                                    <option value="25">25</option>
                                    <option value="30">30</option>
                                    <option value="35">35</option>
                                    <option value="40">40</option>
                                    <option value="45">45</option>
                                    <option value="50">50</option>
                                    <option value="55">55</option>
                                </select>
                                <span>past every hour</span>
                            </div>
                        </div>
                    </div>

                    <!-- daily: time of day -->
                    <div id="bldDailyFields" style="display: none;">
                        <div class="form-inline">
                            <div class="form-group">
                                <label>Time of Day:</label>
                                <input type="time" id="bldTimeOfDayDaily" class="form-control input-sm" value="00:00" style="width: 130px; margin-left: 5px;" />
                            </div>
                        </div>
                        <p class="help-block" style="margin-top: 4px;">Runs every day at the specified time.</p>
                    </div>

                    <!-- weekly: time of day + day of week -->
                    <div id="bldWeeklyFields" style="display: none;">
                        <div class="form-inline">
                            <div class="form-group" style="margin-right: 15px;">
                                <label>Time of Day:</label>
                                <input type="time" id="bldTimeOfDayWeekly" class="form-control input-sm" value="00:00" style="width: 130px; margin-left: 5px;" />
                            </div>
                            <div class="form-group">
                                <label>Day of Week:</label>
                                <select id="bldDayOfWeekSelect" class="form-control input-sm" style="width: 140px; margin-left: 5px;">
                                    <option value="1">Monday</option>
                                    <option value="2">Tuesday</option>
                                    <option value="3">Wednesday</option>
                                    <option value="4">Thursday</option>
                                    <option value="5">Friday</option>
                                    <option value="6">Saturday</option>
                                    <option value="0">Sunday</option>
                                </select>
                            </div>
                        </div>
                        <p class="help-block" style="margin-top: 4px;">Runs every week on the selected day at the specified time.</p>
                    </div>

                    <!-- monthly: time of day + day of month -->
                    <div id="bldMonthlyFields" style="display: none;">
                        <div class="form-inline">
                            <div class="form-group" style="margin-right: 15px;">
                                <label>Time of Day:</label>
                                <input type="time" id="bldTimeOfDayMonthly" class="form-control input-sm" value="00:00" style="width: 130px; margin-left: 5px;" />
                            </div>
                            <div class="form-group">
                                <label>Day of Month:</label>
                                <select id="bldDayOfMonthSelect" class="form-control input-sm" style="width: 120px; margin-left: 5px;">
                                    <option value="1">Day 1</option>
                                    <option value="2">Day 2</option>
                                    <option value="3">Day 3</option>
                                    <option value="4">Day 4</option>
                                    <option value="5">Day 5</option>
                                    <option value="6">Day 6</option>
                                    <option value="7">Day 7</option>
                                    <option value="8">Day 8</option>
                                    <option value="9">Day 9</option>
                                    <option value="10">Day 10</option>
                                    <option value="11">Day 11</option>
                                    <option value="12">Day 12</option>
                                    <option value="13">Day 13</option>
                                    <option value="14">Day 14</option>
                                    <option value="15">Day 15</option>
                                    <option value="16">Day 16</option>
                                    <option value="17">Day 17</option>
                                    <option value="18">Day 18</option>
                                    <option value="19">Day 19</option>
                                    <option value="20">Day 20</option>
                                    <option value="21">Day 21</option>
                                    <option value="22">Day 22</option>
                                    <option value="23">Day 23</option>
                                    <option value="24">Day 24</option>
                                    <option value="25">Day 25</option>
                                    <option value="26">Day 26</option>
                                    <option value="27">Day 27</option>
                                    <option value="28">Day 28</option>
                                </select>
                            </div>
                        </div>
                        <div id="bldMonthlyDayWarning" class="text-warning" style="display: none; margin-top: 5px;">
                            <span class="glyphicon glyphicon-warning-sign"></span>
                            Months without day 29, 30, or 31 will have no scheduled run for this expression.
                        </div>
                        <p class="help-block" style="margin-top: 4px;">Runs every month on the selected day at the specified time.</p>
                    </div>

                    <div class="form-group" style="margin-top: 10px;">
                        <label>Generated CRON:</label>
                        <code id="bldCronPreview" class="form-control-static">* * * * *</code>
                        <small id="bldCronDescription" class="text-muted" style="margin-left: 8px;"></small>
                    </div>
                </div>

                <!-- Advanced mode -->
                <div id="scheduleAdvancedFields" style="display: none;">
                    <div class="form-group">
                        <label for="scheduleAdvancedCron">CRON expression (five-field):</label>
                        <input type="text" id="scheduleAdvancedCron" class="form-control input-sm" placeholder="minute hour dayOfMonth month dayOfWeek" style="width: 300px; font-family: monospace;" />
                    </div>
                </div>

                <!-- Timezone selection -->
                <div class="form-group" style="margin-top: 10px;">
                    <label for="scheduleTimezone">Timezone:</label>
                    <select id="scheduleTimezone" class="form-control input-sm" style="width: 200px;">
                        <option value="utc">UTC</option>
                        <option value="vietnam" selected>Vietnam (UTC+07:00)</option>
                    </select>
                </div>

                <!-- Buttons -->
                <div style="margin-top: 15px;">
                    <button id="btnApplySchedule" class="btn btn-primary btn-sm">Apply temporary schedule</button>
                    <button id="btnRestoreSchedule" class="btn btn-default btn-sm">Restore default schedule</button>
                    <span id="scheduleSpinner" style="display: none; margin-left: 10px;"><img src="/hangfire/img/ajax-loader.gif" alt="Loading..." /></span>
                </div>

                <!-- Status/Error messages -->
                <div id="scheduleResult" style="margin-top: 10px;"></div>
            </div>
        </div>

        <!-- TAB 4: BOOTSTRAP EXPLORER -->
        <div class="tab-pane" id="bootstrapTab">
            <div id="bsRequestList">
                <div class="card">
                    <h4>Bootstrap Requests</h4>
                    <div class="filter-panel form-inline" style="margin-bottom: 15px;">
                        <div class="form-group" style="margin-right: 10px;">
                            <label for="bsFilterRule">Rule Name: </label>
                            <input type="text" id="bsFilterRule" class="form-control input-sm" placeholder="e.g. CRM.Partners" />
                        </div>
                        <div class="form-group" style="margin-right: 10px;">
                            <label for="bsFilterStatus">Status: </label>
                            <select id="bsFilterStatus" class="form-control input-sm">
                                <option value="">-- All --</option>
                                <option value="queued">queued</option>
                                <option value="running">running</option>
                                <option value="completed">completed</option>
                                <option value="failed">failed</option>
                                <option value="recovery_pending">recovery_pending</option>
                            </select>
                        </div>
                        <button id="btnBsFilter" class="btn btn-primary btn-sm">Filter</button>
                    </div>
                    <div class="table-responsive">
                        <table class="table table-striped table-hover" id="bsRequestTable">
                            <thead>
                                <tr>
                                    <th>Rule Name</th>
                                    <th>Status</th>
                                    <th>Health</th>
                                    <th>Children</th>
                                    <th>Latest Event</th>
                                    <th>Created At</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody id="bsTbody">
                                <tr><td colspan="7" class="text-center">Loading requests...</td></tr>
                            </tbody>
                        </table>
                    </div>
                    <div id="bsPagination" class="sync-pagination"></div>
                </div>
            </div>

            <!-- Detail Panel -->
            <div class="card" id="bsDetailPanel" style="display: none;">
                <h4>
                    Request Detail
                    <button id="btnBsBack" class="btn btn-default btn-sm pull-right">Back to List</button>
                </h4>
                <div id="bsDetailContent"></div>
            </div>

            <!-- Confirm Modal -->
            <div class="modal fade" id="bsConfirmModal" tabindex="-1" role="dialog">
                <div class="modal-dialog" role="document">
                    <div class="modal-content">
                        <div class="modal-header">
                            <button type="button" class="close" data-dismiss="modal">&times;</button>
                            <h4 class="modal-title" id="bsConfirmTitle">Confirm Action</h4>
                        </div>
                        <div class="modal-body" id="bsConfirmBody">
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-primary" id="btnBsConfirmOk">Confirm</button>
                            <button type="button" class="btn btn-default" data-dismiss="modal">Cancel</button>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Action Status -->
            <div id="bsActionResult" style="margin-top: 10px;"></div>
        </div>
    </div>
</div>

<!-- Modal for Error Detail -->
<div class="modal fade" id="errorModal" tabindex="-1" role="dialog">
    <div class="modal-dialog modal-lg" role="document">
        <div class="modal-content">
            <div class="modal-header">
                <button type="button" class="close" data-dismiss="modal">&times;</button>
                <h4 class="modal-title">Error detail</h4>
            </div>
            <div class="modal-body">
                <p><strong>Error Code:</strong> <span id="modalErrorCode" class="label label-danger"></span></p>
                <p><strong>Error Message:</strong></p>
                <pre id="modalErrorMessage"></pre>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-default" data-dismiss="modal">Close</button>
            </div>
        </div>
    </div>
</div>

<script src="https://cdn.jsdelivr.net/npm/jquery@1.12.4/dist/jquery.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/bootstrap@3.4.1/dist/js/bootstrap.min.js"></script>
<script>
    (function () {
        var overviewItems = [];
        var overviewCurrentPage = 1;
        var overviewPageSize = 20;
        var logsCurrentPage = 1;
        var logsTotalPages = 1;
        var logsTotalCount = 0;
        var logsPageSize = 20;
        var baseUrl = window.location.pathname.replace(/\/$/, '');
        var bsPageIndex = 1;
        var bsPageSize = 20;
        var bsItems = [];
        var bsFilterRule = '';
        var bsFilterStatus = '';
        var bsSelectedRequestId = null;
        var bsPendingAction = null;
        var bsRefreshTimer = null;

        function startBsRefresh() {
            stopBsRefresh();
            bsRefreshTimer = setInterval(function () {
                if (bsSelectedRequestId) {
                    loadBsDetail(bsSelectedRequestId);
                } else {
                    loadBsRequests();
                }
            }, 10000);
        }

        function stopBsRefresh() {
            if (bsRefreshTimer) { clearInterval(bsRefreshTimer); bsRefreshTimer = null; }
        }

        function formatLag(lagMs) {
            if (lagMs === null || lagMs === undefined) return 'N/A';
            var sec = Math.floor(lagMs / 1000);
            if (sec < 60) return sec + 's';
            var min = Math.floor(sec / 60);
            if (min < 60) return min + 'm ' + (sec % 60) + 's';
            var hr = Math.floor(min / 60);
            return hr + 'h ' + (min % 60) + 'm';
        }

        function formatDate(dtStr) {
            if (!dtStr) return '-';
            var dt = new Date(dtStr);
            return dt.toISOString().replace('T', ' ').substring(0, 19);
        }

        function getHealthBadge(status) {
            var cls = 'badge-disabled';
            if (status === 'Healthy') cls = 'badge-healthy';
            else if (status === 'Degraded') cls = 'badge-degraded';
            else if (status === 'Failed') cls = 'badge-failed';
            else if (status === 'NeverSynced') cls = 'badge-neversynced';
            return '<span class="badge ' + cls + '">' + (status || 'Unknown') + '</span>';
        }

        function getOutcomeBadge(outcome) {
            var cls = 'label-default';
            if (outcome === 'succeeded') cls = 'label-success';
            else if (outcome === 'no_changes') cls = 'label-info';
            else if (outcome === 'failed') cls = 'label-danger';
            else if (outcome === 'requires_full_resync') cls = 'label-danger';
            else if (outcome === 'skipped_locked') cls = 'label-warning';
            return '<span class="label ' + cls + '">' + (outcome || '-') + '</span>';
        }

        function escapeHtml(text) {
            if (!text) return '';
            var div = document.createElement('div');
            div.appendChild(document.createTextNode(text));
            return div.innerHTML;
        }

        function loadBsRequests() {
            $.getJSON(baseUrl + '/api/bootstrap')
                .done(function (items) {
                    bsItems = items || [];
                    bsPageIndex = 1;
                    renderBsPage();
                })
                .fail(function () {
                    bsItems = [];
                    $('#bsTbody').html('<tr><td colspan="7" class="text-center text-danger">Failed to load.</td></tr>');
                    $('#bsPagination').empty();
                });
        }

        function renderBsPage() {
            var filtered = bsItems;
            if (bsFilterRule) {
                var ruleLower = bsFilterRule.toLowerCase();
                filtered = filtered.filter(function (r) { return (r.ruleName || '').toLowerCase().indexOf(ruleLower) >= 0; });
            }
            if (bsFilterStatus) {
                filtered = filtered.filter(function (r) { return r.requestStatus === bsFilterStatus; });
            }

            var totalCount = filtered.length;
            var totalPages = Math.max(1, Math.ceil(totalCount / bsPageSize));
            bsPageIndex = Math.min(bsPageIndex, totalPages);
            var start = (bsPageIndex - 1) * bsPageSize;
            var pageItems = filtered.slice(start, start + bsPageSize);

            var html = '';
            if (totalCount === 0) {
                html = '<tr><td colspan="7" class="text-center">No bootstrap requests found.</td></tr>';
            } else {
                pageItems.forEach(function (item) {
                    var healthBadge = getHealthBadge(item.health);
                    var progress = item.completedChildren + '/' + item.totalChildren;
                    html += '<tr>' +
                        '<td><strong>' + escapeHtml(item.ruleName || '-') + '</strong></td>' +
                        '<td>' + escapeHtml(item.requestStatus || '-') + '</td>' +
                        '<td>' + healthBadge + '</td>' +
                        '<td>' + progress + '</td>' +
                        '<td>' + escapeHtml(item.latestEventType || '-') + '</td>' +
                        '<td>' + formatDate(item.createdAt) + '</td>' +
                        '<td><button class="btn btn-xs btn-info btn-bs-inspect" data-id="' + (item.requestId || '') + '">Inspect</button></td>' +
                        '</tr>';
                });
            }
            $('#bsTbody').html(html);
            renderPagination('#bsPagination', {
                prefix: 'bootstrap',
                currentPage: bsPageIndex,
                totalPages: totalPages,
                totalCount: totalCount,
                pageSize: bsPageSize
            });
        }

        function loadBsDetail(requestId) {
            $.getJSON(baseUrl + '/api/bootstrap/detail', { requestId: requestId })
                .done(function (detail) {
                    if (!detail) {
                        $('#bsDetailContent').html('<div class="alert alert-danger">Request not found.</div>');
                        return;
                    }
                    bsSelectedRequestId = requestId;
                    $('#bsRequestList').hide();
                    $('#bsDetailPanel').show();

                    var html = '<div class="row"><div class="col-md-6">';
                    html += '<h5>Request Info</h5>';
                    html += '<p><strong>Request ID:</strong> ' + escapeHtml(detail.requestId || '-') + '</p>';
                    html += '<p><strong>Rule:</strong> ' + escapeHtml(detail.ruleName || '-') + '</p>';
                    html += '<p><strong>Status:</strong> ' + escapeHtml(detail.requestStatus || '-') + '</p>';
                    html += '<p><strong>Type:</strong> ' + escapeHtml(detail.bootstrapType || '-') + '</p>';
                    html += '</div>';

                    if (detail.parent) {
                        var p = detail.parent;
                        html += '<div class="col-md-6">';
                        html += '<h5>Parent Info</h5>';
                        html += '<p><strong>Status:</strong> ' + escapeHtml(p.status || '-') + '</p>';
                        html += '<p><strong>Stage Table:</strong> ' + escapeHtml(p.stagingTableName || '-') + '</p>';
                        html += '<p><strong>C0:</strong> ' + escapeHtml(p.baselineVersion || '-') + ' / <strong>C1:</strong> ' + escapeHtml(p.watermarkVersion || '-') + '</p>';
                        html += '<p><strong>Rows Staged:</strong> ' + p.rowsStaged + '</p>';
                        html += '<p><strong>Phase Job:</strong> ' + escapeHtml(p.phaseJobId || '-') + ' (' + escapeHtml(p.hangfireJobState || 'Unknown') + ')</p>';
                        if (p.errorCode) html += '<p><strong>Error:</strong> ' + escapeHtml(p.errorCode) + ' \u2014 ' + escapeHtml(p.errorMessage || '') + '</p>';
                        html += '</div>';
                    }
                    html += '</div>';

                    html += '<div style="margin-bottom: 15px;">';
                    if (detail.parent && detail.parent.canReconcile) {
                        html += '<button class="btn btn-xs btn-warning btn-bs-action" data-action="reconcile" data-parent-id="' + (detail.parent.parentId || '') + '" data-status="' + escapeHtml(detail.parent.status || '') + '">Reconcile Parent</button> ';
                    }
                    if (detail.parent && detail.parent.canCancel) {
                        html += '<button class="btn btn-xs btn-danger btn-bs-action" data-action="cancel" data-parent-id="' + (detail.parent.parentId || '') + '" data-status="' + escapeHtml(detail.parent.status || '') + '">Cancel</button> ';
                    }
                    html += '</div>';

                    if (detail.children && detail.children.length > 0) {
                        html += '<h5>Children (' + detail.children.length + ')</h5>';
                        html += '<table class="table table-condensed table-striped"><thead><tr><th>Seq</th><th>Status</th><th>Rows</th><th>Job State</th><th>Actions</th></tr></thead><tbody>';
                        detail.children.forEach(function (c) {
                            html += '<tr><td>' + c.sequence + '</td><td>' + escapeHtml(c.status || '-') + '</td><td>' + c.rowsRead + '</td><td>' + escapeHtml(c.hangfireJobState || 'Unknown') + '</td>';
                            html += '<td>';
                            if (c.canRetry) {
                                html += '<button class="btn btn-xs btn-warning btn-bs-action" data-action="retry" data-child-id="' + (c.childId || '') + '" data-parent-id="' + (detail.parent ? detail.parent.parentId : '') + '" data-status="' + escapeHtml(c.status || '') + '">Retry</button>';
                            }
                            html += '</td></tr>';
                        });
                        html += '</tbody></table>';
                    } else {
                        html += '<p>No children</p>';
                    }

                    if (detail.timeline && detail.timeline.length > 0) {
                        html += '<h5>Diagnostic Timeline (last ' + detail.timeline.length + ')</h5>';
                        html += '<table class="table table-condensed table-striped"><thead><tr><th>Time</th><th>Entity</th><th>Event</th><th>Status</th><th>Code</th><th>Message</th></tr></thead><tbody>';
                        detail.timeline.forEach(function (e) {
                            html += '<tr><td>' + formatDate(e.occurredAt) + '</td><td>' + escapeHtml(e.entityType || '-') + '</td><td>' + escapeHtml(e.eventType || '-') + '</td><td>' + escapeHtml(e.fromStatus || '-') + ' &rarr; ' + escapeHtml(e.toStatus || '-') + '</td><td>' + escapeHtml(e.diagnosticCode || '') + '</td><td>' + escapeHtml(e.sanitizedMessage || '') + '</td></tr>';
                        });
                        html += '</tbody></table>';
                    }

                    $('#bsDetailContent').html(html);
                })
                .fail(function () {
                    $('#bsDetailContent').html('<div class="alert alert-danger">Failed to load detail.</div>');
                });
        }

        $(document).on('click', '.btn-bs-inspect', function () {
            var id = $(this).data('id');
            loadBsDetail(id);
        });

        $('#btnBsBack').click(function () {
            $('#bsDetailPanel').hide();
            $('#bsRequestList').show();
            bsSelectedRequestId = null;
            renderBsPage();
        });

        $('#btnBsFilter').click(function () {
            bsFilterRule = $('#bsFilterRule').val() || '';
            bsFilterStatus = $('#bsFilterStatus').val() || '';
            bsPageIndex = 1;
            renderBsPage();
        });

        $(document).on('click', '.btn-bs-action', function () {
            var action = $(this).data('action');
            var childId = $(this).data('child-id');
            var parentId = $(this).data('parent-id');
            var status = $(this).data('status') || 'unknown';

            var title = action.charAt(0).toUpperCase() + action.slice(1);
            var body = '<p>Current status: <strong>' + escapeHtml(status) + '</strong></p>';
            if (action === 'cancel') {
                body += '<div class="alert alert-danger"><strong>Warning:</strong> This will stop the parent at the next safe checkpoint and trigger staging cleanup. Active work cannot be resumed.</div>';
                body += '<p>Cancel parent <strong>' + escapeHtml(parentId || '') + '</strong>?</p>';
            } else if (action === 'retry') {
                body += '<p>Retry child <strong>' + escapeHtml(childId || '') + '</strong> (parent ' + escapeHtml(parentId || '') + ')?</p>';
            } else {
                body += '<p>' + escapeHtml(action.charAt(0).toUpperCase() + action.slice(1)) + ' parent <strong>' + escapeHtml(parentId || '') + '</strong>?</p>';
            }

            $('#bsConfirmTitle').text(title);
            $('#bsConfirmBody').html(body);
            bsPendingAction = { action: action, childId: childId, parentId: parentId };
            $('#bsConfirmModal').modal('show');
        });

        $('#btnBsConfirmOk').click(function () {
            if (!bsPendingAction) return;
            $('#bsConfirmModal').modal('hide');

            var payload = {};
            var url = baseUrl + '/api/bootstrap/';

            if (bsPendingAction.action === 'retry') {
                url += 'children/retry';
                payload.childId = bsPendingAction.childId;
                payload.parentId = bsPendingAction.parentId;
            } else if (bsPendingAction.action === 'cancel') {
                url += 'parents/cancel';
                payload.parentId = bsPendingAction.parentId;
            } else {
                url += 'parents/reconcile';
                payload.parentId = bsPendingAction.parentId;
            }

            $.ajax({
                type: 'POST',
                url: url,
                data: JSON.stringify(payload),
                contentType: 'application/json'
            })
            .done(function (result) {
                var cls = 'alert alert-success';
                var msg = bsPendingAction.action + ' accepted';
                if (result && result.status === 'conflict') {
                    cls = 'alert alert-warning';
                    msg = result.message || 'Conflict: action not eligible';
                }
                $('#bsActionResult').attr('class', cls).text(msg);
                if (bsSelectedRequestId) loadBsDetail(bsSelectedRequestId);
                else loadBsRequests();
            })
            .fail(function (xhr) {
                var msg = 'Action failed';
                try { var err = JSON.parse(xhr.responseText); msg = err.message || msg; } catch (e) {}
                $('#bsActionResult').attr('class', 'alert alert-danger').text(msg);
            });

            bsPendingAction = null;
        });

        function getPageWindow(currentPage, totalPages) {
            var pages = [];
            if (totalPages <= 10) {
                for (var i = 1; i <= totalPages; i++) pages.push(i);
                return pages;
            }

            if (currentPage <= 7) {
                for (var left = 1; left <= 10; left++) pages.push(left);
                pages.push('ellipsis');
                return pages;
            }

            pages.push(1);
            pages.push('ellipsis');

            var start = Math.max(2, currentPage - 4);
            var end = Math.min(totalPages - 1, currentPage + 4);
            for (var mid = start; mid <= end; mid++) pages.push(mid);

            if (end < totalPages - 1) pages.push('ellipsis');
            pages.push(totalPages);
            return pages;
        }

        function renderPagination(targetSelector, state) {
            var totalPages = Math.max(1, state.totalPages || 1);
            var currentPage = Math.min(Math.max(1, state.currentPage || 1), totalPages);
            var totalCount = state.totalCount || 0;
            var pageSize = state.pageSize || 20;
            var prefix = state.prefix;
            var pages = getPageWindow(currentPage, totalPages);
            var html = '<div class="sync-page-list">' +
                '<button type="button" class="sync-page-btn" data-page-target="' + prefix + '" data-page="first" ' + (currentPage <= 1 ? 'disabled' : '') + '>&laquo;</button>' +
                '<button type="button" class="sync-page-btn" data-page-target="' + prefix + '" data-page="prev" ' + (currentPage <= 1 ? 'disabled' : '') + '>&lsaquo;</button>';

            pages.forEach(function (page) {
                if (page === 'ellipsis') {
                    html += '<span class="sync-page-ellipsis">...</span>';
                    return;
                }

                html += '<button type="button" class="sync-page-btn ' + (page === currentPage ? 'active' : '') + '" data-page-target="' + prefix + '" data-page="' + page + '">' + page + '</button>';
            });

            html += '<button type="button" class="sync-page-btn" data-page-target="' + prefix + '" data-page="next" ' + (currentPage >= totalPages ? 'disabled' : '') + '>&rsaquo;</button>' +
                '<button type="button" class="sync-page-btn" data-page-target="' + prefix + '" data-page="last" ' + (currentPage >= totalPages ? 'disabled' : '') + '>&raquo;</button>' +
                '</div>' +
                '<span class="sync-page-summary">' + currentPage + ' of ' + totalPages + ' pages</span>' +
                '<span class="sync-page-summary">(' + totalCount + ' items)</span>' +
                '<label class="sync-page-size">' +
                    '<select class="form-control input-sm" data-page-size-target="' + prefix + '">' +
                        '<option value="10"' + (pageSize === 10 ? ' selected' : '') + '>10</option>' +
                        '<option value="20"' + (pageSize === 20 ? ' selected' : '') + '>20</option>' +
                        '<option value="50"' + (pageSize === 50 ? ' selected' : '') + '>50</option>' +
                        '<option value="100"' + (pageSize === 100 ? ' selected' : '') + '>100</option>' +
                    '</select>' +
                    '<span>Items per page</span>' +
                '</label>';

            $(targetSelector).html(html);
        }

        function resolvePage(prefix, requestedPage) {
            var currentPage = prefix === 'overview' ? overviewCurrentPage : logsCurrentPage;
            var totalPages = prefix === 'overview'
                ? Math.max(1, Math.ceil(overviewItems.length / overviewPageSize))
                : logsTotalPages;

            if (requestedPage === 'first') return 1;
            if (requestedPage === 'prev') return Math.max(1, currentPage - 1);
            if (requestedPage === 'next') return Math.min(totalPages, currentPage + 1);
            if (requestedPage === 'last') return totalPages;

            var parsed = parseInt(requestedPage, 10);
            return parsed > 0 ? Math.min(parsed, totalPages) : currentPage;
        }

        function renderOverviewPage() {
            var totalCount = overviewItems.length;
            var totalPages = Math.max(1, Math.ceil(totalCount / overviewPageSize));
            overviewCurrentPage = Math.min(overviewCurrentPage, totalPages);
            var start = (overviewCurrentPage - 1) * overviewPageSize;
            var pageRows = overviewItems.slice(start, start + overviewPageSize);
            var html = '';

            if (totalCount === 0) {
                html = '<tr><td colspan="12" class="text-center">No table sync configs found.</td></tr>';
            } else {
                pageRows.forEach(function (row, index) {
                    html += '<tr>' +
                        '<td class="col-no">' + (start + index + 1) + '</td>' +
                        '<td><strong>' + (row.ruleName || '-') + '</strong></td>' +
                        '<td>' + (row.syncTier || '-') + '</td>' +
                        '<td>' + (row.syncStatus || '-') + '</td>' +
                        '<td>' + getHealthBadge(row.healthStatus) + '</td>' +
                        '<td>' + formatLag(row.lastSyncLagMs) + '</td>' +
                        '<td>' + formatDate(row.lastSuccessAt) + '</td>' +
                        '<td>' + (row.consecutiveFailureCount || 0) + '</td>' +
                        '<td>' + getOutcomeBadge(row.latestRunOutcome) + '</td>' +
                        '<td>' + (row.latestRunRowsUpserted !== null ? row.latestRunRowsUpserted : '-') + '</td>' +
                        '<td>' + (row.latestRunDurationMs !== null ? row.latestRunDurationMs + 'ms' : '-') + '</td>' +
                        '<td>' + (row.lastErrorMessage ? '<button class="btn btn-xs btn-danger btn-err" data-code="' + (row.lastErrorCode || '') + '" data-msg="' + encodeURIComponent(row.lastErrorMessage || '') + '">View</button>' : '-') + '</td>' +
                        '</tr>';
                });
            }

            $('#overviewTbody').html(html);
            renderPagination('#overviewPagination', {
                prefix: 'overview',
                currentPage: overviewCurrentPage,
                totalPages: totalPages,
                totalCount: totalCount,
                pageSize: overviewPageSize
            });
        }

        function loadOverview() {
            $.getJSON(baseUrl + '/api/overview')
                .done(function (data) {
                    overviewItems = data && data.items ? data.items : (data || []);
                    overviewCurrentPage = 1;
                    renderOverviewPage();
                })
                .fail(function () {
                    overviewItems = [];
                    $('#overviewTbody').html('<tr><td colspan="12" class="text-center text-danger">Failed to load overview data.</td></tr>');
                    $('#overviewPagination').empty();
                });
        }

        function loadLogs(page) {
            logsCurrentPage = page || 1;
            var params = {
                pageIndex: logsCurrentPage,
                pageSize: logsPageSize,
                ruleName: $('#filterRule').val(),
                outcome: $('#filterOutcome').val()
            };

            var fromVal = $('#filterFrom').val();
            if (fromVal) params.from = new Date(fromVal).toISOString();
            var toVal = $('#filterTo').val();
            if (toVal) params.to = new Date(toVal).toISOString();

            $.getJSON(baseUrl + '/api/logs', params)
                .done(function (res) {
                    var html = '';
                    var items = res.items || [];
                    logsTotalPages = res.totalPages || 1;
                    logsTotalCount = res.totalCount || 0;
                    var start = (logsCurrentPage - 1) * logsPageSize;

                    if (items.length === 0) {
                        html = '<tr><td colspan="11" class="text-center">No log entries found.</td></tr>';
                    } else {
                        items.forEach(function (log, index) {
                            var chk = (log.checkpointBefore !== null ? log.checkpointBefore : '-') + ' &rarr; ' + (log.checkpointAfter !== null ? log.checkpointAfter : '-');
                            html += '<tr>' +
                                '<td class="col-no">' + (start + index + 1) + '</td>' +
                                '<td>' + formatDate(log.startedAt) + '</td>' +
                                '<td><strong>' + (log.ruleName || '-') + '</strong></td>' +
                                '<td>' + (log.mode || '-') + '</td>' +
                                '<td>' + getOutcomeBadge(log.outcome) + '</td>' +
                                '<td>' + log.rowsRead + '</td>' +
                                '<td>' + log.rowsUpserted + '</td>' +
                                '<td>' + log.rowsDeactivated + ' / ' + log.rowsDeleted + '</td>' +
                                '<td>' + chk + '</td>' +
                                '<td>' + (log.durationMs !== null ? log.durationMs + 'ms' : '-') + '</td>' +
                                '<td>' + (log.errorMessage || log.errorCode ? '<button class="btn btn-xs btn-default btn-err" data-code="' + (log.errorCode || '') + '" data-msg="' + encodeURIComponent(log.errorMessage || '') + '">Detail</button>' : '-') + '</td>' +
                                '</tr>';
                        });
                    }
                    $('#logsTbody').html(html);
                    renderPagination('#logsPagination', {
                        prefix: 'logs',
                        currentPage: logsCurrentPage,
                        totalPages: logsTotalPages,
                        totalCount: logsTotalCount,
                        pageSize: logsPageSize
                    });
                })
                .fail(function () {
                    $('#logsTbody').html('<tr><td colspan="11" class="text-center text-danger">Failed to load sync logs.</td></tr>');
                    $('#logsPagination').empty();
                });
        }

        $(document).on('click', '.btn-err', function () {
            var code = $(this).attr('data-code') || '';
            var msg = decodeURIComponent($(this).attr('data-msg') || '');
            $('#modalErrorCode').text(code || 'N/A');
            document.getElementById('modalErrorMessage').textContent = msg || '(No error message)';
            $('#errorModal').modal('show');
        });

        $('#btnRefresh').click(function () {
            loadOverview();
            if ($('#logsTab').hasClass('active')) loadLogs(logsCurrentPage);
        });

        $('#btnFilter').click(function () {
            loadLogs(1);
        });

        $('#btnReset').click(function () {
            $('#filterRule').val('');
            $('#filterOutcome').val('');
            $('#filterFrom').val('');
            $('#filterTo').val('');
            loadLogs(1);
        });

        $(document).on('click', '[data-page-target]', function () {
            var prefix = $(this).attr('data-page-target');
            var requestedPage = $(this).attr('data-page');
            var page = resolvePage(prefix, requestedPage);

            if (prefix === 'overview') {
                overviewCurrentPage = page;
                renderOverviewPage();
                return;
            }

            if (prefix === 'bootstrap') {
                bsPageIndex = page;
                renderBsPage();
                return;
            }

            loadLogs(page);
        });

        $(document).on('change', '[data-page-size-target]', function () {
            var prefix = $(this).attr('data-page-size-target');
            var pageSize = parseInt($(this).val(), 10) || 20;

            if (prefix === 'overview') {
                overviewPageSize = pageSize;
                overviewCurrentPage = 1;
                renderOverviewPage();
                return;
            }

            if (prefix === 'bootstrap') {
                bsPageSize = pageSize;
                bsPageIndex = 1;
                renderBsPage();
                return;
            }

            logsPageSize = pageSize;
            loadLogs(1);
        });

        function loadScheduleState() {
            $.getJSON(baseUrl + '/api/schedule')
                .done(function (resp) {
                    var state = resp.state || {};
                    window._scheduleState = state;
                    renderSelectedScheduleState();

                    if (resp.requestToken) {
                        window._scheduleRequestToken = resp.requestToken;
                    }

                    // Clear prior messages
                    var resultDiv = document.getElementById('scheduleResult');
                    resultDiv.textContent = '';
                })
                .fail(function () {
                    document.getElementById('currentRecurringJob').textContent = 'Unavailable';
                    document.getElementById('currentCron').textContent = 'Unavailable';
                    document.getElementById('currentTimezone').textContent = 'Unavailable';
                    document.getElementById('nextExecution').textContent = 'Unavailable';
                });
        }

        function getSelectedTier() {
            return $('#scheduleTier').val() || 'Hot';
        }

        function getSelectedScheduleState() {
            var state = window._scheduleState || {};
            return getSelectedTier() === 'Cold' ? (state.cold || {}) : (state.hot || {});
        }

        function renderSelectedScheduleState() {
            var state = getSelectedScheduleState();
            document.getElementById('currentRecurringJob').textContent = state.recurringJobId || 'N/A';
            document.getElementById('currentCron').textContent = state.cronExpression || 'N/A';
            document.getElementById('currentTimezone').textContent = state.timeZoneLabel || 'N/A';
            document.getElementById('nextExecution').textContent = state.nextExecutionUtc
                ? new Date(state.nextExecutionUtc).toISOString().replace('T', ' ').substring(0, 19)
                : 'N/A';
            document.getElementById('defaultCron').textContent = state.defaultCronExpression || 'N/A';

            var resultDiv = document.getElementById('scheduleResult');
            resultDiv.textContent = '';
            resultDiv.className = '';
        }

        function buildCronFromBuilder() {
            var freq = $('#scheduleFrequency').val();
            var cron = '';
            var desc = '';

            // Always hide all warnings first, individual cases will show as needed
            $('#bldDayWarning').hide();
            $('#bldMonthlyDayWarning').hide();

            switch (freq) {
                case 'minutes':
                    var interval = parseInt($('#bldEveryNInterval').val(), 10);
                    if (!interval || interval < 1) interval = 10;
                    cron = '*/' + interval + ' * * * *';
                    desc = 'Every ' + interval + ' minute(s)';
                    break;

                case 'hourly':
                    var minPast = $('#bldEachHourMinute').val() || '0';
                    cron = minPast + ' * * * *';
                    desc = 'At minute ' + minPast + ' past every hour';
                    break;

                case 'daily':
                    var timeDaily = $('#bldTimeOfDayDaily').val() || '00:00';
                    var partsDaily = timeDaily.split(':');
                    var minDaily = parseInt(partsDaily[1], 10) || 0;
                    var hrDaily = parseInt(partsDaily[0], 10) || 0;
                    cron = minDaily + ' ' + hrDaily + ' * * *';
                    desc = 'Every day at ' + timeDaily;
                    break;

                case 'weekly':
                    var timeWeekly = $('#bldTimeOfDayWeekly').val() || '00:00';
                    var partsWeekly = timeWeekly.split(':');
                    var minWeekly = parseInt(partsWeekly[1], 10) || 0;
                    var hrWeekly = parseInt(partsWeekly[0], 10) || 0;
                    var dowVal = $('#bldDayOfWeekSelect').val() || '1';
                    cron = minWeekly + ' ' + hrWeekly + ' * * ' + dowVal;
                    var dowNames = { '0': 'Sunday', '1': 'Monday', '2': 'Tuesday', '3': 'Wednesday', '4': 'Thursday', '5': 'Friday', '6': 'Saturday' };
                    desc = 'Every ' + (dowNames[dowVal] || 'day') + ' at ' + timeWeekly;
                    break;

                case 'monthly':
                    var timeMonthly = $('#bldTimeOfDayMonthly').val() || '00:00';
                    var partsMonthly = timeMonthly.split(':');
                    var minMonthly = parseInt(partsMonthly[1], 10) || 0;
                    var hrMonthly = parseInt(partsMonthly[0], 10) || 0;
                    var domVal = $('#bldDayOfMonthSelect').val() || '1';
                    cron = minMonthly + ' ' + hrMonthly + ' ' + domVal + ' * *';
                    desc = 'Day ' + domVal + ' of every month at ' + timeMonthly;
                    break;

                default: // custom
                    var minute = $('#bldMinute').val() || '*';
                    var hour = $('#bldHour').val() || '*';
                    var dom = $('#bldDayOfMonth').val() || '*';
                    var month = $('#bldMonth').val() || '*';
                    var dow = $('#bldDayOfWeek').val() || '*';
                    cron = minute + ' ' + hour + ' ' + dom + ' ' + month + ' ' + dow;

                    var dayVal = parseInt(dom, 10);
                    if (dayVal >= 29 && dayVal <= 31) {
                        $('#bldDayWarning').show();
                    }
                    desc = '';
                    break;
            }

            $('#bldCronPreview').text(cron || '* * * * *');
            $('#bldCronDescription').text(desc);
            return cron;
        }

        // ── Frequency preset switching ──
        function switchScheduleFrequency() {
            var freq = $('#scheduleFrequency').val();
            $('#bldCustomFields').toggle(freq === 'custom');
            $('#bldMinutesFields').toggle(freq === 'minutes');
            $('#bldHourlyFields').toggle(freq === 'hourly');
            $('#bldDailyFields').toggle(freq === 'daily');
            $('#bldWeeklyFields').toggle(freq === 'weekly');
            $('#bldMonthlyFields').toggle(freq === 'monthly');
            buildCronFromBuilder();
        }
        $('#scheduleFrequency').on('change', function () {
            switchScheduleFrequency();
        });

        function getCronFromActiveMode() {
            if ($('#modeBuilder').is(':checked')) {
                return buildCronFromBuilder();
            }
            return $('#scheduleAdvancedCron').val() || '';
        }

        function postSchedule(action, cronExpr, timezoneKey) {
            $('#btnApplySchedule').prop('disabled', true);
            $('#btnRestoreSchedule').prop('disabled', true);
            $('#scheduleSpinner').show();

            var tier = getSelectedTier();
            var payload = { action: action, tier: tier };
            if (action === 'apply') {
                payload.cronExpression = cronExpr;
                payload.timeZoneKey = timezoneKey;
            }

            var headers = {};
            if (window._scheduleRequestToken) {
                headers['RequestVerificationToken'] = window._scheduleRequestToken;
            }

            $.ajax({
                type: 'POST',
                url: baseUrl + '/api/schedule',
                data: JSON.stringify(payload),
                contentType: 'application/json',
                headers: headers
            })
                .done(function (resp) {
                    var resultDiv = document.getElementById('scheduleResult');
                    resultDiv.className = 'alert alert-success';
                    resultDiv.textContent = tier + ' schedule updated successfully.';
                    loadScheduleState();
                })
                .fail(function (xhr) {
                    var resultDiv = document.getElementById('scheduleResult');
                    resultDiv.className = 'alert alert-danger';
                    try {
                        var err = JSON.parse(xhr.responseText);
                        resultDiv.textContent = err.error || 'An error occurred.';
                    } catch (e) {
                        resultDiv.textContent = 'Failed to update schedule.';
                    }
                })
                .always(function () {
                    $('#btnApplySchedule').prop('disabled', false);
                    $('#btnRestoreSchedule').prop('disabled', false);
                    $('#scheduleSpinner').hide();
                });
        }

        // Mode switching: clear inactive fields
        function switchScheduleMode() {
            if ($('#modeBuilder').is(':checked')) {
                $('#scheduleBuilderFields').show();
                $('#scheduleAdvancedFields').hide();
                $('#scheduleAdvancedCron').val('');
                switchScheduleFrequency();
            } else {
                $('#scheduleBuilderFields').hide();
                $('#scheduleAdvancedFields').show();
                $('#bldMinute').val('*'); $('#bldHour').val('*');
                $('#bldDayOfMonth').val('*'); $('#bldMonth').val('*'); $('#bldDayOfWeek').val('*');
                $('#bldDayWarning').hide();
                $('#bldMonthlyDayWarning').hide();
                $('#bldCronPreview').text('* * * * *');
                $('#bldCronDescription').text('');
            }
        }
        $('input[name="scheduleMode"]').on('change click', function () {
            switchScheduleMode();
        });

        // Rebuild CRON preview when any builder field changes
        $('#bldMinute, #bldHour, #bldDayOfMonth, #bldMonth, #bldDayOfWeek, '
            + '#bldEveryNInterval, #bldEachHourMinute, #bldTimeOfDayDaily, '
            + '#bldTimeOfDayWeekly, #bldTimeOfDayMonthly, #bldDayOfWeekSelect, #bldDayOfMonthSelect')
            .on('input change', function () {
                buildCronFromBuilder();
            });

        // Apply schedule
        $('#btnApplySchedule').click(function () {
            var cron = getCronFromActiveMode();
            var tz = $('#scheduleTimezone').val();
            if (!cron) {
                document.getElementById('scheduleResult').className = 'alert alert-danger';
                document.getElementById('scheduleResult').textContent = 'CRON expression is required.';
                return;
            }
            postSchedule('apply', cron, tz);
        });

        // Restore default
        $('#btnRestoreSchedule').click(function () {
            postSchedule('restoreDefault');
        });

        $('#scheduleTier').on('change', function () {
            renderSelectedScheduleState();
        });

        $('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
            if ($(e.target).attr('href') === '#logsTab') {
                stopBsRefresh();
                loadLogs(1);
            }
            if ($(e.target).attr('href') === '#scheduleTab') {
                stopBsRefresh();
                loadScheduleState();
                switchScheduleMode();
                switchScheduleFrequency();
            }
            if ($(e.target).attr('href') === '#bootstrapTab') {
                loadBsRequests();
                startBsRefresh();
            }
        });

        loadOverview();
        switchScheduleFrequency();
    })();
</script>
</body>
</html>
""";
    }
}
