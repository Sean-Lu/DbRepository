using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Sean.Core.DbRepository.Util;

public static class ExpressionUtil
{
    public static Expression<Func<TEntity, object>> CreateFieldExpression<TEntity>(IEnumerable<string> fieldNames)
    {
        Expression body = Expression.Constant(fieldNames);
        ParameterExpression param = Expression.Parameter(typeof(TEntity), "entity");
        var fieldExpression = Expression.Lambda<Func<TEntity, object>>(body, param);
        return fieldExpression;
    }
}