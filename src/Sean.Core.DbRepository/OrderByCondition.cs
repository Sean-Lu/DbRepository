using Sean.Core.DbRepository.Extensions;
using System;
using System.Linq.Expressions;

namespace Sean.Core.DbRepository
{
    public class OrderByCondition
    {
        public OrderByCondition(string orderBy)
        {
            OrderBy = orderBy;
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

        public OrderByType Type { get; }
        public string[] Fields { get; }

        public string OrderBy { get; }

        public OrderByCondition Next { get; set; }

        public static OrderByCondition Create<TEntity>(OrderByType type, Expression<Func<TEntity, object>> fieldExpression, OrderByCondition next = null)
        {
            return new OrderByCondition(type, fieldExpression.GetFieldNames().ToArray(), next);
        }
    }

    public class OrderByCondition<TEntity> : OrderByCondition
    {
        public OrderByCondition(string orderBy) : base(orderBy)
        {
        }

        public OrderByCondition(OrderByType type, string field, OrderByCondition next = null) : base(type, field, next)
        {
        }

        public OrderByCondition(OrderByType type, string[] fields, OrderByCondition next = null) : base(type, fields, next)
        {
        }

        public static OrderByCondition Create(OrderByType type, Expression<Func<TEntity, object>> fieldExpression, OrderByCondition next = null)
        {
            return OrderByCondition.Create(type, fieldExpression, next);
        }
    }
}
