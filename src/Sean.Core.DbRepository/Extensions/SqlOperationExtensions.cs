using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sean.Core.DbRepository.Extensions
{
    public static class SqlOperationExtensions
    {
        public static string ToSqlString(this SqlOperation operation)
        {
            switch (operation)
            {
                case SqlOperation.None:
                    return string.Empty;
                case SqlOperation.Equal:
                    return "=";
                case SqlOperation.NotEqual:
                    return "<>";
                case SqlOperation.Less:
                    return "<";
                case SqlOperation.LessOrEqual:
                    return "<=";
                case SqlOperation.Greater:
                    return ">";
                case SqlOperation.GreaterOrEqual:
                    return ">=";
                case SqlOperation.In:
                    return "IN";
                case SqlOperation.NotIn:
                    return "NOT IN";
                case SqlOperation.Like:
                    return "LIKE";
                default:
                    throw new NotSupportedException(operation.ToString());
            }
        }
    }
}
