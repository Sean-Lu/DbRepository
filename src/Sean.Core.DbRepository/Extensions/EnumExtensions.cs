using System;
using System.Linq.Expressions;

namespace Sean.Core.DbRepository.Extensions
{
    public static class EnumExtensions
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
                    throw new NotImplementedException($"未实现的SQL操作符类型：{operation.ToString()}");
            }
        }

        public static string ToSqlString(this ExpressionType expressionType)
        {
            switch (expressionType)
            {
                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.NotEqual:
                    return "<>";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return "AND";
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return "OR";
                default:
                    throw new NotImplementedException($"未实现的表达式树节点的节点类型：{expressionType}");
            }
        }

        public static string ToSqlString(this Include include)
        {
            switch (include)
            {
                case Include.None:
                    return string.Empty;
                case Include.Left:
                    return "(";
                case Include.Right:
                    return ")";
                default:
                    throw new NotSupportedException(include.ToString());
            }
        }

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

        public static string ToSqlString(this WhereSqlKeyword keyword)
        {
            switch (keyword)
            {
                case WhereSqlKeyword.None:
                    return string.Empty;
                case WhereSqlKeyword.And:
                    return "AND";
                case WhereSqlKeyword.Or:
                    return "OR";
                default:
                    throw new NotSupportedException(keyword.ToString());
            }
        }
    }
}
