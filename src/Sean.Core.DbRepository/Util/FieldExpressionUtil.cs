using System;
using System.Linq.Expressions;

namespace Sean.Core.DbRepository.Util;

public class FieldExpressionUtil
{
    public static Expression<Func<TEntity, object>> Create<TEntity>(Expression<Func<TEntity, object>> fieldExpression)
    {
        return fieldExpression;
    }
    public static Expression<Func<TEntity, object>> Create<TEntity>(bool condition, Expression<Func<TEntity, object>> trueFieldExpression, Expression<Func<TEntity, object>> falseFieldExpression)
    {
        return condition ? trueFieldExpression : falseFieldExpression;
    }
}