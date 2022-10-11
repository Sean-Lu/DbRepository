using Sean.Core.DbRepository.Extensions;
using System;
using System.Linq.Expressions;

namespace Sean.Core.DbRepository
{
    public class OrderByCondition
    {
        public OrderByCondition()
        {
        }

        public OrderByCondition(OrderByType type, string field, OrderByCondition next = null)
        {
            Type = type;
            Fields = new[] { field };
            Next = next;
        }

        public OrderByCondition(OrderByType type, string[] fields, OrderByCondition next = null)
        {
            Type = type;
            Fields = fields;
            Next = next;
        }

        public OrderByType Type { get; set; }
        public string[] Fields { get; set; }

        public OrderByCondition Next { get; set; }

        public static OrderByCondition New<TEntity>(OrderByType type, Expression<Func<TEntity, object>> fieldExpression, OrderByCondition next = null)
        {
            var orderBy = new OrderByCondition
            {
                Type = type,
                Fields = fieldExpression.GetFieldNames().ToArray(),
                Next = next
            };
            return orderBy;
        }
    }

    public class OrderByCondition<TEntity> : OrderByCondition
    {
        public OrderByCondition()
        {
        }

        public OrderByCondition(OrderByType type, string field, OrderByCondition next = null) : base(type, field, next)
        {
        }

        public OrderByCondition(OrderByType type, string[] fields, OrderByCondition next = null) : base(type, fields, next)
        {
        }

        public static OrderByCondition New(OrderByType type, Expression<Func<TEntity, object>> fieldExpression, OrderByCondition next = null)
        {
            return OrderByCondition.New(type, fieldExpression, next);
        }
    }
}
