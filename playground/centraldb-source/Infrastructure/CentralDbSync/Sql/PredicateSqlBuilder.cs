using Application.Features.CentralDbSync.Mapping;
using Dapper;

namespace Infrastructure.CentralDbSync.Sql;

public sealed class PredicateSqlBuilder
{
    public string BuildWhereClause(
        IReadOnlyList<ColumnPredicate> predicates,
        string defaultAlias,
        DynamicParameters parameters,
        string parameterPrefix)
    {
        if (predicates.Count == 0)
            return string.Empty;

        var fragments = new List<string>();
        for (var i = 0; i < predicates.Count; i++)
        {
            var predicate = predicates[i];
            var columnSql = SqlServerSqlBuilder.QuoteSqlServerColumnReference(predicate.Column, defaultAlias);
            var parameterName = $"{parameterPrefix}{i}";

            if (predicate.Operator is PredicateOperator.In or PredicateOperator.NotIn)
            {
                var values = AsEnumerable(predicate.Value).ToList();
                var parameterNames = values
                    .Select((_, valueIndex) => $"{parameterName}_{valueIndex}")
                    .ToList();
                for (var valueIndex = 0; valueIndex < values.Count; valueIndex++)
                {
                    parameters.Add(parameterNames[valueIndex], values[valueIndex]);
                }

                var inOperator = predicate.Operator == PredicateOperator.In ? "IN" : "NOT IN";
                fragments.Add($"{columnSql} {inOperator} ({string.Join(", ", parameterNames.Select(name => "@" + name))})");
                continue;
            }

            fragments.Add(predicate.Operator switch
            {
                PredicateOperator.Eq => $"{columnSql} = @{parameterName}",
                PredicateOperator.Neq => $"{columnSql} <> @{parameterName}",
                PredicateOperator.IsNull => $"{columnSql} IS NULL",
                PredicateOperator.IsNotNull => $"{columnSql} IS NOT NULL",
                PredicateOperator.Gt => $"{columnSql} > @{parameterName}",
                PredicateOperator.Gte => $"{columnSql} >= @{parameterName}",
                PredicateOperator.Lt => $"{columnSql} < @{parameterName}",
                PredicateOperator.Lte => $"{columnSql} <= @{parameterName}",
                _ => throw new ArgumentOutOfRangeException(nameof(predicate.Operator), predicate.Operator, "Unsupported predicate operator.")
            });

            if (predicate.Operator is not PredicateOperator.IsNull and not PredicateOperator.IsNotNull)
                parameters.Add(parameterName, predicate.Value);
        }

        return string.Join(" AND ", fragments);
    }

    private static IEnumerable<object?> AsEnumerable(object? value)
        => value is System.Collections.IEnumerable enumerable and not string
            ? enumerable.Cast<object?>()
            : [];
}
