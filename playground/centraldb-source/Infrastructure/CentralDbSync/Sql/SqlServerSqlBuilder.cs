using Application.Features.CentralDbSync.Mapping;
using Dapper;

namespace Infrastructure.CentralDbSync.Sql;

public sealed class SqlServerSqlBuilder(PredicateSqlBuilder predicateSqlBuilder)
{
    public SelectSql BuildBootstrapSelect(TableMappingRule rule)
    {
        var parameters = new DynamicParameters();
        var selectList = BuildSelectList(rule);
        var sql = $@"SELECT {string.Join(", ", selectList.Select(c => c.Sql))}
            FROM {QuoteSqlServerTable(rule.Source.PrimaryTable)} AS {QuoteIdentifier(rule.Source.PrimaryAlias)} WITH (READPAST)";

        var whereClause = BuildReadWhereClause(rule, parameters);

        if (!string.IsNullOrWhiteSpace(whereClause))
            sql += $"{Environment.NewLine}WHERE {whereClause}";

        return new SelectSql(sql, selectList.Select(c => c.Alias).ToList(), parameters);
    }

    public SelectSql BuildKeysetBootstrapSelect(
        TableMappingRule rule,
        object? afterKey,
        int batchSize)
    {
        var parameters = new DynamicParameters();

        // Share the select list with the CT path so bootstrap batches and CT delta rows
        // are keyed by identical column aliases for every downstream consumer.
        var selectList = BuildSelectList(rule);

        // Build WHERE clause (ReadFilter + ReadFilterSql + keyset)
        var whereFragments = new List<string>();

        var predicateWhere = predicateSqlBuilder.BuildWhereClause(
            rule.Source.ReadFilter,
            rule.Source.PrimaryAlias,
            parameters,
            "readFilter");

        if (!string.IsNullOrWhiteSpace(predicateWhere))
            whereFragments.Add(predicateWhere);

        if (!string.IsNullOrWhiteSpace(rule.Source.ReadFilterSql))
            whereFragments.Add("(" + rule.Source.ReadFilterSql + ")");

        // Keyset filter (non-null afterKey means we need a WHERE condition)
        var pkColumn = rule.Source.PrimaryKey[0];
        if (afterKey is not null)
        {
            parameters.Add("afterKey", afterKey);
            whereFragments.Add($"{QuoteSqlServerColumnReference(pkColumn, rule.Source.PrimaryAlias)} > @afterKey");
        }

        // ActivePredicate: filter rows by active flag (e.g. t0.IsCustomer = 1)
        var activeWhere = predicateSqlBuilder.BuildWhereClause(
            rule.Source.ActivePredicate,
            rule.Source.PrimaryAlias,
            parameters,
            "activeFilter");
        if (!string.IsNullOrWhiteSpace(activeWhere))
            whereFragments.Add(activeWhere);

        parameters.Add("BatchSize", batchSize);

        var whereClause = whereFragments.Count > 0
            ? $"{Environment.NewLine}WHERE {string.Join(" AND ", whereFragments)}"
            : "";

        var sql = $@"SELECT TOP (@BatchSize) {string.Join(", ", selectList.Select(c => c.Sql))}
            FROM {QuoteSqlServerTable(rule.Source.PrimaryTable)} AS {QuoteIdentifier(rule.Source.PrimaryAlias)} WITH (READCOMMITTED){whereClause}
            ORDER BY {QuoteSqlServerColumnReference(pkColumn, rule.Source.PrimaryAlias)}";

        return new SelectSql(sql, selectList.Select(c => c.Alias).ToList(), parameters);
    }

    public SelectSql BuildChangeTrackingSelect(TableMappingRule rule)
    {
        var parameters = new DynamicParameters();
        var selectList = BuildSelectList(rule);
        var primaryKeySelect = rule.Source.PrimaryKey
            .Select((pk, index) => $"CT.{QuoteIdentifier(pk)} AS {QuoteIdentifier(GetCtPrimaryKeyAlias(index))}");
        var selectedColumns = primaryKeySelect
            .Concat(selectList.Select(c => c.Sql))
            .Prepend("CT.SYS_CHANGE_VERSION")
            .Prepend("CT.SYS_CHANGE_OPERATION");
        var joinClause = string.Join(" AND ", rule.Source.PrimaryKey.Select(
            pk => $"{QuoteIdentifier(rule.Source.PrimaryAlias)}.{QuoteIdentifier(pk)} = CT.{QuoteIdentifier(pk)}"));

        var sql = $@"SELECT {string.Join(", ", selectedColumns)}
            FROM CHANGETABLE(CHANGES {QuoteSqlServerTable(rule.Source.PrimaryTable)}, @checkpoint) AS CT
            LEFT JOIN {QuoteSqlServerTable(rule.Source.PrimaryTable)} AS {QuoteIdentifier(rule.Source.PrimaryAlias)} WITH (READPAST) ON {joinClause}
            WHERE CT.SYS_CHANGE_VERSION <= @upperWatermark";

        var whereClause = BuildReadWhereClause(rule, parameters);

        if (!string.IsNullOrWhiteSpace(whereClause))
            sql += $"{Environment.NewLine}  AND (CT.SYS_CHANGE_OPERATION = 'D' OR ({whereClause}))";

        sql += $"{Environment.NewLine}ORDER BY CT.SYS_CHANGE_VERSION";

        foreach (var pk in rule.Source.PrimaryKey)
        {
            sql += $", CT.{QuoteIdentifier(pk)}";
        }

        return new SelectSql(sql, selectList.Select(c => c.Alias).ToList(), parameters);
    }

    public static string GetCtPrimaryKeyAlias(int index) => $"__ct_pk_{index}";

    private string BuildReadWhereClause(TableMappingRule rule, DynamicParameters parameters)
    {
        var fragments = new List<string>();
        var predicateWhere = predicateSqlBuilder.BuildWhereClause(
            rule.Source.ReadFilter,
            rule.Source.PrimaryAlias,
            parameters,
            "readFilter");

        if (!string.IsNullOrWhiteSpace(predicateWhere))
            fragments.Add(predicateWhere);
        if (!string.IsNullOrWhiteSpace(rule.Source.ReadFilterSql))
            fragments.Add("(" + rule.Source.ReadFilterSql + ")");

        return string.Join(" AND ", fragments);
    }

    public static string QuoteSqlServerTable(string logicalTable)
        => $"[dbo].[{logicalTable.Replace("]", "]]")}]";

    public static string QuoteSqlServerColumnReference(string column, string defaultAlias)
    {
        var parts = column.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            1 => $"{QuoteIdentifier(defaultAlias)}.{QuoteIdentifier(parts[0])}",
            2 => $"{QuoteIdentifier(parts[0])}.{QuoteIdentifier(parts[1])}",
            _ => throw new InvalidOperationException($"Column reference '{column}' is invalid.")
        };
    }

    private static IReadOnlyList<SelectColumn> BuildSelectList(TableMappingRule rule)
    {
        var columns = new List<SelectColumn>();
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in rule.Columns)
        {
            if (mapping.IsActiveFlag || !string.IsNullOrWhiteSpace(mapping.Transform))
                continue;

            var alias = !string.IsNullOrWhiteSpace(mapping.SourceExpression)
                ? mapping.TargetColumn
                : GetSourceAlias(mapping.SourceColumn!);
            if (!aliases.Add(alias))
                continue;

            var expression = !string.IsNullOrWhiteSpace(mapping.SourceExpression)
                ? mapping.SourceExpression
                : QuoteSqlServerColumnReference(mapping.SourceColumn!, rule.Source.PrimaryAlias);
            columns.Add(new SelectColumn($"{expression} AS {QuoteIdentifier(alias)}", alias));
        }

        foreach (var mapping in rule.Columns.Where(c => !string.IsNullOrWhiteSpace(c.Transform)))
        {
            foreach (var dependency in mapping.TransformDependsOn)
            {
                var alias = GetSourceAlias(dependency);
                if (!aliases.Add(alias))
                    continue;
                columns.Add(new SelectColumn(
                    $"{QuoteSqlServerColumnReference(dependency, rule.Source.PrimaryAlias)} AS {QuoteIdentifier(alias)}",
                    alias));
            }
        }

        foreach (var predicate in rule.Source.ActivePredicate)
        {
            var alias = GetSourceAlias(predicate.Column);
            if (!aliases.Add(alias))
                continue;

            columns.Add(new SelectColumn(
                $"{QuoteSqlServerColumnReference(predicate.Column, rule.Source.PrimaryAlias)} AS {QuoteIdentifier(alias)}",
                alias));
        }

        return columns;
    }

    private static string GetSourceAlias(string column)
        => column.Contains('.') ? column[(column.LastIndexOf('.') + 1)..] : column;

    private static string QuoteIdentifier(string identifier)
        => $"[{identifier.Replace("]", "]]")}]";

    private sealed record SelectColumn(string Sql, string Alias);
}

public sealed record SelectSql(
    string Sql,
    IReadOnlyList<string> ColumnAliases,
    DynamicParameters Parameters);
