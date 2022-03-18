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
        public static OrderByCondition CreateNew<TProperty>(OrderByType type, Expression<Func<TEntity, TProperty>> fieldExpression, OrderByCondition next = null)
        {
            return OrderByConditionBuilder<TEntity>.Build(type, fieldExpression, next);
        }
    }

    public class OrderByConditionBuilder
    {
        public static OrderByCondition Build<TEntity, TProperty>(OrderByType type, Expression<Func<TEntity, TProperty>> fieldExpression, OrderByCondition next = null)
        {
            var orderByCondition = new OrderByCondition
            {
                Type = type,
                Fields = fieldExpression.GetMemberNames().ToArray(),
                Next = next
            };
            return orderByCondition;
        }
    }

    public class OrderByConditionBuilder<TEntity>
    {
        public static OrderByCondition Build<TProperty>(OrderByType type, Expression<Func<TEntity, TProperty>> fieldExpression, OrderByCondition next = null)
        {
            return OrderByConditionBuilder.Build(type, fieldExpression, next);
        }
    }

    public static class OrderByConditionExtensions
    {
        public static void Resolve(this OrderByCondition orderByCondition, SqlFactory sqlFactory)
        {
            if (orderByCondition?.Fields == null || !orderByCondition.Fields.Any())
            {
                return;
            }

            sqlFactory.OrderByField(orderByCondition.Type, orderByCondition.Fields);

            if (orderByCondition.Next != null)
            {
                orderByCondition.Next.Resolve(sqlFactory);
            }
        }
    }
}
