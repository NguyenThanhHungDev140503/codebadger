using System.Linq.Expressions;

namespace Application.Common.Expressions;

internal static class PredicateExpressionExtensions
{
    public static Expression<Func<T, bool>> Or<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var parameter = left.Parameters[0];

        var rightBody = new ReplaceParameterVisitor(
                right.Parameters[0],
                parameter)
            .Visit(right.Body)!;

        return Expression.Lambda<Func<T, bool>>(
            Expression.OrElse(left.Body, rightBody),
            parameter);
    }

    private sealed class ReplaceParameterVisitor(
        ParameterExpression source,
        ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == source ? target : node;
        }
    }
}