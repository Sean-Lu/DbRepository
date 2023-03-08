using System;
using System.Linq;

namespace Sean.Core.DbRepository.Extensions
{
    public static class OrderByConditionExtensions
    {
        public static void Resolve(this OrderByCondition orderBy, Action<OrderByType, string[], string> orderByField)
        {
            if (orderBy == null)
            {
                return;
            }

            orderByField(orderBy.Type, orderBy.Fields, orderBy.OrderBy);

            orderBy.Next?.Resolve(orderByField);
        }
    }
}
