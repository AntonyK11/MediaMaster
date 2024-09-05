using System.Linq.Expressions;

namespace MediaMaster.Extensions;

internal static class ExpressionsExtension
{
    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
    {
        Expression right = new RebindParameterVisitor(expr2.Parameters[0], expr1.Parameters[0]).Visit(expr2.Body);
        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(expr1.Body, right), expr1.Parameters);
    }

    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        Expression right = new RebindParameterVisitor(expr2.Parameters[0], expr1.Parameters[0]).Visit(expr2.Body);
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(expr1.Body, right), expr1.Parameters);
    }

    public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> expr)
    {
        return Expression.Lambda<Func<T, bool>>(Expression.Not(expr.Body), expr.Parameters);
    }
}

public class RebindParameterVisitor(
    ParameterExpression oldParameter,
    ParameterExpression newParameter)
    : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == oldParameter ? newParameter : base.VisitParameter(node);
    }
}