using System;
using System.Linq.Expressions;

namespace Sean.Core.DbRepository
{
    public class OrderByConditionBuilder
    {
        public static OrderByCondition Build<TEntity>(OrderByType type, Expression<Func<TEntity, object>> fieldExpression, OrderByCondition next = null)
        {
            return OrderByCondition<TEntity>.New(type, fieldExpression, next);
        }
    }

    public class OrderByConditionBuilder<TEntity>
    {
        public static OrderByCondition Build(OrderByType type, Expression<Func<TEntity, object>> fieldExpression, OrderByCondition next = null)
        {
            return OrderByConditionBuilder.Build(type, fieldExpression, next);
        }
    }
}
