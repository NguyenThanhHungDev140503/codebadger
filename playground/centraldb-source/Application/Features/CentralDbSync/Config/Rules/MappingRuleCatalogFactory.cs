using Application.Features.CentralDbSync.Mapping;
using Application.Features.CentralDbSync.Validation;

namespace Application.Features.CentralDbSync.Config.Rules;

internal static class MappingRuleCatalogFactory
{
    public const int CompanyId = 2;

    public static TableMappingRule Create(
        string ruleName,
        string sourceTable,
        string sourcePrimaryKey,
        string targetTable,
        string targetPrimaryKey,
        IReadOnlyList<ColumnMapping> columns,
        bool filterCompany = false,
        string? activeFilterColumn = null,
        IReadOnlyList<ColumnPredicate>? activePredicate = null,
        IReadOnlyList<ColumnPredicate>? readFilter = null,
        string? readFilterSql = null,
        string syncTier = "Cold",
        int expectedSyncIntervalMinutes = 2,
        int maxAllowedLagMinutes = 5,
        string ownershipScope = "erp")
    {
        SyncGuard.AssertValidSyncTier(syncTier, nameof(syncTier));

        return new()
        {
            RuleName = ruleName,
            Source = new SourceSpec
            {
                PrimaryTable = sourceTable,
                PrimaryKey = [sourcePrimaryKey],
                ReadFilter = BuildReadFilter(filterCompany, activeFilterColumn, readFilter),
                ReadFilterSql = readFilterSql,
                ActivePredicate = activePredicate ?? []
            },
            Target = new TargetSpec
            {
                Schema = "ref",
                Table = targetTable,
                PrimaryKey = [targetPrimaryKey]
            },
            Columns = columns,
            SyncTier = syncTier,
            ExpectedSyncInterval = TimeSpan.FromMinutes(expectedSyncIntervalMinutes),
            MaxAllowedLag = TimeSpan.FromMinutes(maxAllowedLagMinutes),
            OwnershipScope = ownershipScope,
            Enabled = true
        };
    }

    public static ColumnMapping Map(string targetColumn, string targetType, string sourceColumn) => new()
    {
        TargetColumn = targetColumn,
        TargetType = targetType,
        SourceColumn = sourceColumn
    };

    public static ColumnMapping MapPk(string targetColumn, string targetType, string sourceColumn) => new()
    {
        TargetColumn = targetColumn,
        TargetType = targetType,
        SourceColumn = sourceColumn,
        IsPrimaryKey = true
    };

    public static ColumnMapping ActiveFlag(string targetColumn, string targetType = "boolean") => new()
    {
        TargetColumn = targetColumn,
        TargetType = targetType,
        IsActiveFlag = true
    };

    public static ColumnPredicate Eq(string sourceColumn, object? value) => new()
    {
        Column = sourceColumn,
        Operator = PredicateOperator.Eq,
        Value = value
    };

    public static ColumnPredicate In(string sourceColumn, IReadOnlyList<object?> values) => new()
    {
        Column = sourceColumn,
        Operator = PredicateOperator.In,
        Value = values
    };

    private static IReadOnlyList<ColumnPredicate> BuildReadFilter(
        bool filterCompany,
        string? activeFilterColumn,
        IReadOnlyList<ColumnPredicate>? extraReadFilter)
    {
        var filters = new List<ColumnPredicate>();

        if (filterCompany)
        {
            filters.Add(new ColumnPredicate
            {
                Column = "CompanyId",
                Operator = PredicateOperator.Eq,
                Value = CompanyId
            });
        }

        if (!string.IsNullOrWhiteSpace(activeFilterColumn))
        {
            filters.Add(new ColumnPredicate
            {
                Column = activeFilterColumn,
                Operator = PredicateOperator.Eq,
                Value = true
            });
        }

        if (extraReadFilter is not null)
            filters.AddRange(extraReadFilter);

        return filters;
    }
}
