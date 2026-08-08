namespace Infrastructure.CentralDbSync;

using Application.Features.CentralDbSync.Abstractions;
using Application.Features.CentralDbSync.Mapping;
using Dapper;
using Microsoft.Data.SqlClient;

public sealed class SqlServerCtHealthCheck(
    string connectionString,
    IMappingRuleProvider ruleProvider)
    : ISqlServerCtHealthCheck
{
    public async Task<CtHealthResult> CheckAsync(string sourceTable, CancellationToken ct)
    {
        if (!ruleProvider.TryGet(sourceTable, out var rule))
        {
            return new CtHealthResult
            {
                SourceTable = sourceTable,
                IsCtEnabled = false,
                ErrorMessage = $"Table '{sourceTable}' is not registered in mapping rules."
            };
        }

        try
        {
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(ct);

            var tableName = $"[dbo].[{rule.Source.PrimaryTable}]";

            // Check sys.change_tracking_tables for CT enablement
            var ctRow = await conn.QueryFirstOrDefaultAsync(
                @"SELECT
                    is_track_columns_updated_on,
                    min_valid_version,
                    begin_version
                  FROM sys.change_tracking_tables
                  WHERE object_id = OBJECT_ID(@tableName)",
                new { tableName });

            if (ctRow == null)
            {
                return new CtHealthResult
                {
                    SourceTable = sourceTable,
                    SchemaQualifiedName = tableName,
                    IsCtEnabled = false,
                    ErrorMessage =
                        $"Change Tracking is NOT enabled on {tableName}. " +
                        "Run: ALTER TABLE {table} ENABLE CHANGE_TRACKING"
                };
            }

            var currentVersion = await conn.ExecuteScalarAsync<long>(
                $"SELECT CHANGE_TRACKING_CURRENT_VERSION()");

            var minValid = await conn.ExecuteScalarAsync<long?>(
                $"SELECT CHANGE_TRACKING_MIN_VALID_VERSION(OBJECT_ID(@tableName))",
                new { tableName });

            return new CtHealthResult
            {
                SourceTable = sourceTable,
                SchemaQualifiedName = tableName,
                IsCtEnabled = true,
                CurrentVersion = currentVersion == 0 ? null : currentVersion,
                MinValidVersion = minValid
            };
        }
        catch (Exception ex)
        {
            return new CtHealthResult
            {
                SourceTable = sourceTable,
                IsCtEnabled = false,
                ErrorMessage =
                    $"Failed to check CT status: {ex.Message}"
            };
        }
    }
}
