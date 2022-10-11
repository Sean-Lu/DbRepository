using System;
using System.Linq;

namespace Sean.Core.DbRepository.Extensions
{
    public static class OrderByConditionExtensions
    {
        public static void Resolve(this OrderByCondition orderBy, Action<OrderByType, string[]> orderByField)
        {
            if (orderBy?.Fields == null || !orderBy.Fields.Any())
            {
                return;
            }

            orderByField(orderBy.Type, orderBy.Fields);

            orderBy.Next?.Resolve(orderByField);
        }
    }
}
