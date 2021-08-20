using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sean.Core.DbRepository.Extensions
{
    public static class OrderByTypeExtensions
    {
        public static string ToSqlString(this OrderByType orderByType)
        {
            switch (orderByType)
            {
                case OrderByType.Asc:
                    return "ASC";
                case OrderByType.Desc:
                    return "DESC";
                default:
                    throw new NotSupportedException(orderByType.ToString());
            }
        }
    }
}
