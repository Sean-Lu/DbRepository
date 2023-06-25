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
}