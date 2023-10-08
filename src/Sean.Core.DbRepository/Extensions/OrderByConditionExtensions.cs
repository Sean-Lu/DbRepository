using System;

namespace Sean.Core.DbRepository.Extensions;

public static class OrderByConditionExtensions
{
    public static void Resolve(this OrderByCondition orderBy, Action<OrderByCondition> orderByAction)
    {
        if (orderBy == null)
        {
            return;
        }

        orderByAction(orderBy);

        orderBy.Next?.Resolve(orderByAction);
    }
}