using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository
{
    public static class ExpressionUtil
    {
        public static Expression<T> Compose<T>(Expression<T> first, Expression<T> second, Func<Expression, Expression, Expression> merge)
        {
            return first.Compose(second, merge);
        }

        public static Expression<Func<TEntity, object>> CreateFieldExpression<TEntity>(IEnumerable<string> fieldNames)
        {
            Expression body = Expression.Constant(fieldNames);
            ParameterExpression param = Expression.Parameter(typeof(TEntity), "entity");
            var fieldExpression = Expression.Lambda<Func<TEntity, object>>(body, param);
            return fieldExpression;
        }
    }
}
