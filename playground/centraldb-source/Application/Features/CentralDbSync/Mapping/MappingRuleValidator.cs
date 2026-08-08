using Application.Features.CentralDbSync.Validation;

namespace Application.Features.CentralDbSync.Mapping;

public sealed class MappingRuleValidator(IValueTransformerRegistry transformerRegistry)
{
    private static readonly HashSet<string> SupportedTargetTypes =
    [
        "text",
        "integer",
        "bigint",
        "boolean",
        "numeric",
        "timestamp",
        "timestamptz",
        "date"
    ];

    public void ValidateAll(IEnumerable<TableMappingRule> rules)
    {
        foreach (var rule in rules)
        {
            Validate(rule);
        }
    }

    public void Validate(TableMappingRule rule)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(rule.RuleName))
            errors.Add("Rule name is required.");
        if (string.IsNullOrWhiteSpace(rule.Source.PrimaryTable))
            errors.Add("Source.PrimaryTable is required.");
        if (string.IsNullOrWhiteSpace(rule.Source.PrimaryAlias))
            errors.Add("Source.PrimaryAlias is required.");
        if (rule.Source.PrimaryKey.Count == 0)
            errors.Add("Source.PrimaryKey must contain at least one column.");
        if (rule.Source.Joins.Count != 0)
            errors.Add("Source.Joins is outside Stage 1-6 scope; only single-source rules are supported.");
        if (string.IsNullOrWhiteSpace(rule.Target.Table))
            errors.Add("Target.Table is required.");
        if (rule.Target.PrimaryKey.Count == 0)
            errors.Add("Target.PrimaryKey must contain at least one column.");
        if (rule.Columns.Count == 0)
            errors.Add("Columns must contain at least one mapping.");
        ValidateSyncTier(rule.SyncTier, errors);

        ValidateColumns(rule, errors);
        ValidatePredicates(rule.Source.ReadFilter, "Source.ReadFilter", errors);
        ValidateReadFilterSql(rule.Source.ReadFilterSql, errors);
        ValidatePredicates(rule.Source.ActivePredicate, "Source.ActivePredicate", errors);

        if (errors.Count != 0)
            throw new InvalidOperationException(
                $"Invalid central DB sync mapping rule '{rule.RuleName}': {string.Join(" ", errors)}");
    }

    private void ValidateColumns(TableMappingRule rule, List<string> errors)
    {
        var duplicateTargetColumns = rule.Columns
            .GroupBy(c => c.TargetColumn, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicateTargetColumns.Count != 0)
            errors.Add("Duplicate target columns: " + string.Join(", ", duplicateTargetColumns) + ".");

        var activeFlagCount = rule.Columns.Count(c => c.IsActiveFlag);
        if (activeFlagCount > 1)
            errors.Add("Only one IsActiveFlag column is allowed.");

        var mappedPrimaryKeys = rule.Columns
            .Where(c => c.IsPrimaryKey)
            .Select(c => c.TargetColumn)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var pk in rule.Target.PrimaryKey)
        {
            if (!mappedPrimaryKeys.Contains(pk))
                errors.Add($"Target primary key '{pk}' must have a matching IsPrimaryKey column mapping.");
        }

        foreach (var column in rule.Columns)
        {
            if (string.IsNullOrWhiteSpace(column.TargetColumn))
                errors.Add("Column.TargetColumn is required.");
            if (string.IsNullOrWhiteSpace(column.TargetType))
                errors.Add($"Column '{column.TargetColumn}' must declare TargetType.");
            else if (!SupportedTargetTypes.Contains(column.TargetType))
                errors.Add($"Column '{column.TargetColumn}' has unsupported TargetType '{column.TargetType}'.");

            var producerCount = CountProducer(column);
            if (column.IsActiveFlag)
            {
                if (producerCount != 0)
                    errors.Add($"Active flag column '{column.TargetColumn}' must not declare SourceColumn, SourceExpression, or Transform.");
            }
            else if (producerCount != 1)
            {
                errors.Add($"Column '{column.TargetColumn}' must declare exactly one producer.");
            }

            if (!string.IsNullOrWhiteSpace(column.Transform))
                transformerRegistry.Resolve(column.Transform);
            if (string.IsNullOrWhiteSpace(column.Transform) && column.TransformDependsOn.Count != 0)
                errors.Add($"Column '{column.TargetColumn}' has TransformDependsOn but no Transform.");
        }
    }

    private static int CountProducer(ColumnMapping column)
    {
        var count = 0;
        if (!string.IsNullOrWhiteSpace(column.SourceColumn)) count++;
        if (!string.IsNullOrWhiteSpace(column.SourceExpression)) count++;
        if (!string.IsNullOrWhiteSpace(column.Transform)) count++;
        return count;
    }

    private static void ValidateSyncTier(string syncTier, List<string> errors)
    {
        try
        {
            SyncGuard.AssertValidSyncTier(syncTier, nameof(syncTier));
        }
        catch (ArgumentException ex)
        {
            errors.Add(ex.Message);
        }
    }

    private static void ValidatePredicates(
        IReadOnlyList<ColumnPredicate> predicates,
        string scope,
        List<string> errors)
    {
        foreach (var predicate in predicates)
        {
            if (string.IsNullOrWhiteSpace(predicate.Column))
                errors.Add($"{scope} contains a predicate without Column.");

            var requiresSequence = predicate.Operator is PredicateOperator.In or PredicateOperator.NotIn;
            var requiresNull = predicate.Operator is PredicateOperator.IsNull or PredicateOperator.IsNotNull;
            if (requiresSequence && !IsNonStringEnumerable(predicate.Value))
                errors.Add($"{scope}.{predicate.Column} uses {predicate.Operator} but Value is not a collection.");
            if (requiresNull && predicate.Value is not null)
                errors.Add($"{scope}.{predicate.Column} uses {predicate.Operator} and must not declare Value.");
            if (!requiresNull && predicate.Value is null)
                errors.Add($"{scope}.{predicate.Column} uses {predicate.Operator} and must declare Value.");
        }
    }

    private static bool IsNonStringEnumerable(object? value)
        => value is System.Collections.IEnumerable and not string;

    private static void ValidateReadFilterSql(string? readFilterSql, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(readFilterSql))
            return;

        if (readFilterSql.Contains(';')
            || readFilterSql.Contains("--", StringComparison.Ordinal)
            || readFilterSql.Contains("/*", StringComparison.Ordinal)
            || readFilterSql.Contains("*/", StringComparison.Ordinal))
        {
            errors.Add("Source.ReadFilterSql must be a single predicate without comments or statement separators.");
        }
    }
}
