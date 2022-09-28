using System;
using System.Linq;
using System.Linq.Expressions;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository
{
    public class OrderByCondition
    {
        public OrderByType Type { get; set; }
        public string[] Fields { get; set; }

        public OrderByCondition Next { get; set; }
    }

    public class OrderByCondition<TEntity> : OrderByCondition
    {
        public static OrderByCondition CreateNew(OrderByType type, Expression<Func<TEntity, object>> fieldExpression, OrderByCondition next = null)
        {
            return OrderByConditionBuilder<TEntity>.Build(type, fieldExpression, next);
        }
    }

    public class OrderByConditionBuilder
    {
        public static OrderByCondition Build<TEntity>(OrderByType type, Expression<Func<TEntity, object>> fieldExpression, OrderByCondition next = null)
        {
            var orderByCondition = new OrderByCondition
            {
                Type = type,
                Fields = fieldExpression.GetFieldNames().ToArray(),
                Next = next
            };
            return orderByCondition;
        }
    }

    public class OrderByConditionBuilder<TEntity>
    {
        public static OrderByCondition Build(OrderByType type, Expression<Func<TEntity, object>> fieldExpression, OrderByCondition next = null)
        {
            return OrderByConditionBuilder.Build(type, fieldExpression, next);
        }
    }

    public static class OrderByConditionExtensions
    {
        public static void Resolve(this OrderByCondition orderByCondition, Action<OrderByType, string[]> orderByField)
        {
            if (orderByCondition?.Fields == null || !orderByCondition.Fields.Any())
            {
                return;
            }

            orderByField(orderByCondition.Type, orderByCondition.Fields);

            if (orderByCondition.Next != null)
            {
                orderByCondition.Next.Resolve(orderByField);
            }
        }
    }
}
