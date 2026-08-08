using Application.Features.CentralDbSync.Mapping;

namespace Infrastructure.CentralDbSync.Sql;

public sealed class UpsertSqlBuilder
{
    public string BuildUpsert(TableMappingRule rule)
    {
        var insertColumns = rule.Columns.Select(c => QuotePgIdentifier(c.TargetColumn))
            .Append(QuotePgIdentifier("source_system"))
            .Append(QuotePgIdentifier("synced_at")).ToList();
        var valueColumns = rule.Columns.Select(c => "@" + c.TargetColumn)
            .Append("@source_system")
            .Append("NOW()").ToList();
        var conflictColumns = rule.Target.PrimaryKey.Select(QuotePgIdentifier);
        var updateColumns = rule.Columns
            .Where(c => !c.IsPrimaryKey)
            .Select(c => $"{QuotePgIdentifier(c.TargetColumn)} = EXCLUDED.{QuotePgIdentifier(c.TargetColumn)}")
            .Append($"{QuotePgIdentifier("synced_at")} = NOW()");

        return $@"INSERT INTO {QuotePgTable(rule.Target.Schema, rule.Target.Table)} ({string.Join(", ", insertColumns)})
            VALUES ({string.Join(", ", valueColumns)})
            ON CONFLICT ({string.Join(", ", conflictColumns)}) DO UPDATE SET {string.Join(", ", updateColumns)}";
    }

    /// <summary>
    /// Builds a parameterized SQL statement that handles the end-of-life of a single
    /// row in the target (PostgreSQL) table identified by its primary key values.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is called during <b>incremental (ChangeTracking) sync</b> when the
    /// source signals that a row was deleted (<c>SYS_CHANGE_OPERATION = 'D'</c>).
    /// Instead of unconditionally deleting the target row, the builder chooses
    /// between two lifecycle strategies based on whether the mapping rule defines
    /// an ActiveFlag column:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <b>Soft-delete (ActiveFlag exists):</b> generates an <c>UPDATE</c> that sets
    /// the ActiveFlag column to <c>false</c> and bumps <c>synced_at</c>. The row
    /// remains in the table but is marked as inactive — preserving referential
    /// integrity for any foreign-key relationships.
    /// </item>
    /// <item>
    /// <b>Hard-delete (no ActiveFlag):</b> generates a <c>DELETE</c> statement
    /// that physically removes the row from the target table. This is only safe
    /// when no other tables reference this row.
    /// </item>
    /// </list>
    /// <para>
    /// The WHERE clause is built from ALL target primary key columns
    /// (e.g. <c>"id" = @id AND "company_id" = @company_id</c>), and the
    /// caller supplies the actual values via Dapper named parameters at execution
    /// time.
    /// </para>
    /// <para>
    /// <b>Contrast with <see cref="BuildLifecycleOrphans"/>:</b>
    /// this method targets a <i>specific row by primary key</i> (used in incremental
    /// sync where the source explicitly tells us which row was deleted), while
    /// <c>BuildLifecycleOrphans</c> uses a <c>NOT IN</c> clause to find <i>all rows
    /// in the target that no longer exist in the source snapshot</i> (used during
    /// bootstrap to clean up rows that disappeared between full reloads).
    /// </para>
    /// </remarks>
    /// <param name="rule">The table mapping rule (source → target column mapping).</param>
    /// <returns>A parameterized SQL statement (UPDATE or DELETE) targeting exactly one row.</returns>
    public string BuildLifecycleByPrimaryKey(TableMappingRule rule)
    {
        // Build WHERE clause from all target primary-key columns:
        // e.g. "id" = @id AND "company_id" = @company_id
        var predicates = rule.Target.PrimaryKey
            .Select(pk => $"{QuotePgIdentifier(pk)} = @{pk}");
        var whereClause = string.Join(" AND ", predicates);

        // Soft-delete path: if the table has an ActiveFlag column, mark the row
        // inactive instead of physically deleting it. This preserves referential
        // integrity for FK relationships and enables audit trails.
        if (TryGetActiveFlagColumn(rule, out var activeFlagColumn))
        {
            return $@"UPDATE {QuotePgTable(rule.Target.Schema, rule.Target.Table)}
                SET {QuotePgIdentifier(activeFlagColumn)} = false,
                    {QuotePgIdentifier("synced_at")} = NOW()
                WHERE {whereClause}";
        }

        // Hard-delete path: no ActiveFlag defined — physically remove the row.
        // Used when the target table has no soft-delete column and no downstream
        // FK dependencies.
        return $@"DELETE FROM {QuotePgTable(rule.Target.Schema, rule.Target.Table)}
            WHERE {whereClause}";
    }

    public string BuildLifecycleOrphans(TableMappingRule rule, string sourceSystemParameterName)
    {
        if (rule.Target.PrimaryKey.Count != 1)
            throw new NotSupportedException("Bootstrap orphan lifecycle currently supports a single-column target primary key.");

        var pk = rule.Target.PrimaryKey[0];
        var whereClause = $@"{QuotePgIdentifier("source_system")} = @{sourceSystemParameterName}
  AND {QuotePgIdentifier(pk)} <> ALL(@snapshotPks)";

        if (TryGetActiveFlagColumn(rule, out var activeFlagColumn))
        {
            return $@"UPDATE {QuotePgTable(rule.Target.Schema, rule.Target.Table)}
SET {QuotePgIdentifier(activeFlagColumn)} = false,
    {QuotePgIdentifier("synced_at")} = NOW()
WHERE {whereClause}";
        }

        return $@"DELETE FROM {QuotePgTable(rule.Target.Schema, rule.Target.Table)}
WHERE {whereClause}";
    }

    public bool HasActiveFlag(TableMappingRule rule)
        => rule.Columns.Any(c => c.IsActiveFlag);

    private static bool TryGetActiveFlagColumn(TableMappingRule rule, out string activeFlagColumn)
    {
        var column = rule.Columns.SingleOrDefault(c => c.IsActiveFlag);
        activeFlagColumn = column?.TargetColumn ?? string.Empty;
        return column is not null;
    }

    private static string QuotePgTable(string schema, string table)
        => $"{QuotePgIdentifier(schema)}.{QuotePgIdentifier(table)}";

    private static string QuotePgIdentifier(string identifier)
        => "\"" + identifier.Replace("\"", "\"\"") + "\"";
}
