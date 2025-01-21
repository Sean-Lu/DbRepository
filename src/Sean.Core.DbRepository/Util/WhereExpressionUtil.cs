using Sean.Core.DbRepository.Extensions;
using System;
using System.Linq.Expressions;

namespace Sean.Core.DbRepository.Util;

public class WhereExpressionUtil
{
    public static Expression<Func<TEntity, bool>> Create<TEntity>(Expression<Func<TEntity, bool>> whereExpression)
    {
        return whereExpression;
    }
    public static Expression<Func<TEntity, bool>> Create<TEntity>(bool condition, Expression<Func<TEntity, bool>> trueWhereExpression, Expression<Func<TEntity, bool>> falseWhereExpression)
    {
        return condition ? trueWhereExpression : falseWhereExpression;
    }

    public static Expression<Func<TEntity, bool>> AndAlsoIF<TEntity>(Expression<Func<TEntity, bool>> whereExpression, bool condition, Expression<Func<TEntity, bool>> mergeWhereExpression)
    {
        return whereExpression.AndAlsoIF(condition, mergeWhereExpression);
    }
    public static Expression<Func<TEntity, bool>> AndAlsoIF<TEntity>(Expression<Func<TEntity, bool>> whereExpression, bool condition, Expression<Func<TEntity, bool>> trueWhereExpression, Expression<Func<TEntity, bool>> falseWhereExpression)
    {
        return whereExpression.AndAlsoIF(condition, trueWhereExpression, falseWhereExpression);
    }

    public static Expression<Func<TEntity, bool>> AndAlso<TEntity>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, bool>> mergeWhereExpression)
    {
        return whereExpression.AndAlso(mergeWhereExpression);
    }

    public static Expression<Func<TEntity, bool>> OrElse<TEntity>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, bool>> mergeWhereExpression)
    {
        return whereExpression.OrElse(mergeWhereExpression);
    }
}