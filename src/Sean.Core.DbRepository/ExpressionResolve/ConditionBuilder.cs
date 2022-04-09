using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository
{
    public static class ConditionBuilder
    {
        public static StringBuilder BuildCondition(MemberInfo memberInfo, WhereClauseAdhesive adhesive, ExpressionType comparison, object value)
        {
            string fieldName = adhesive.SqlAdapter.FormatFieldName(memberInfo.GetFieldName());

            if (value == null)
            {
                if (comparison == ExpressionType.Equal)
                {
                    return new StringBuilder($"{fieldName} is null");
                }
                else if (comparison == ExpressionType.NotEqual)
                {
                    return new StringBuilder($"{fieldName} is not null");
                }
            }

            string parameterName = UniqueParameter(memberInfo, adhesive);
            adhesive.Parameters.Add($"{parameterName}", value);
            return new StringBuilder($"{fieldName} {comparison.ToComparisonSymbol()} {adhesive.SqlAdapter.FormatInputParameter(parameterName)}");
        }

        public static StringBuilder BuildLikeOrEqualCondition(MethodCallExpression methodCallExpression, WhereClauseAdhesive adhesive, bool notEquals = false)
        {
            string symbol;
            string valueSymbol;
            switch (methodCallExpression.Method.Name)
            {
                case "Equals":
                    symbol = notEquals ? "<> {0}" : "= {0}";
                    valueSymbol = "{0}";
                    break;
                case "StartsWith":
                    symbol = "LIKE {0}";
                    valueSymbol = "{0}%";
                    break;
                case "EndsWith":
                    symbol = "LIKE {0}";
                    valueSymbol = "%{0}";
                    break;
                case "Contains":
                    symbol = "LIKE {0}";
                    valueSymbol = "%{0}%";
                    break;
                default:
                    throw new NotSupportedException($"Unsupported method: {methodCallExpression.Method.Name}");
            }

            if (methodCallExpression.Object is MemberExpression memberExpression)
            {
                var memberInfo = memberExpression.Member;
                string fieldName = adhesive.SqlAdapter.FormatFieldName(memberInfo.GetFieldName());
                string parameterName = UniqueParameter(memberInfo, adhesive);
                object value = ConstantExtractor.ParseConstant(methodCallExpression.Arguments[0]);
                adhesive.Parameters.Add($"{parameterName}", string.Format(valueSymbol, value));
                return new StringBuilder(string.Format($"{fieldName} {symbol}", $"{adhesive.SqlAdapter.FormatInputParameter(parameterName)}"));
            }

            throw new NotSupportedException($"Unsupported expression type: {methodCallExpression.Object?.GetType()}");
        }

        public static StringBuilder BuildInCondition(MemberExpression memberExpression, Expression valueExpression, WhereClauseAdhesive adhesive)
        {
            var memberInfo = memberExpression.Member;
            string fieldName = adhesive.SqlAdapter.FormatFieldName(memberInfo.GetFieldName());
            string parameterName = UniqueParameter(memberInfo, adhesive);
            object value = ConstantExtractor.ParseConstant(valueExpression);
            adhesive.Parameters.Add($"{parameterName}", value);
            return new StringBuilder($"{fieldName} IN {adhesive.SqlAdapter.FormatInputParameter(parameterName)}");
        }

        public static StringBuilder BuildIsNullOrEmptyCondition(MethodCallExpression methodCallExpression, WhereClauseAdhesive adhesive, bool reverse = false)
        {
            if (methodCallExpression.Arguments[0] is MemberExpression memberExpression)
            {
                var memberInfo = memberExpression.Member;
                string fieldName = adhesive.SqlAdapter.FormatFieldName(memberInfo.GetFieldName());
                return reverse
                    ? new StringBuilder($"{fieldName} is not null AND {fieldName} <> ''")
                    : new StringBuilder($"({fieldName} is null OR {fieldName} = '')");
            }

            throw new NotSupportedException($"Unsupported expression type: {methodCallExpression.Object?.GetType()}");
        }

        public static string UniqueParameter(MemberInfo mi, WhereClauseAdhesive adhesive)
        {
            return UniqueParameter(mi.Name, adhesive.Parameters);
        }

        public static string UniqueParameter(string paramName, Dictionary<string, object> parameterDic)
        {
            if (!parameterDic.ContainsKey(paramName))
            {
                return paramName;
            }

            int seed = 2;
            string tempParam;
            do
            {
                tempParam = $"{paramName}_{seed++}";
            } while (parameterDic.ContainsKey(tempParam));
            return tempParam;
        }

        private static string ToComparisonSymbol(this ExpressionType expressionType)
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
                default:
                    throw new NotSupportedException($"Unsupported ExpressionType: {expressionType}");
            }
        }
    }
}
