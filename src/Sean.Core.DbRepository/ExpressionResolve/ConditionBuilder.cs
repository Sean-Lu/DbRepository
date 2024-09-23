using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository;

internal static class ConditionBuilder
{
    public static string BuildCondition(ParameterExpression parameterExpression, MemberExpression memberExpression, WhereClauseAdhesive adhesive, ExpressionType comparison, object value, NamingConvention namingConvention, bool reverse = false)
    {
        if (memberExpression.Expression is not ParameterExpression parameterExpression2
            || parameterExpression2.Name != parameterExpression.Name)
        {
            throw new NotSupportedException($"Unsupported MemberExpression: {memberExpression}");
        }

        var memberInfo = memberExpression.Member;
        var fieldName = adhesive.SqlAdapter.FormatFieldName(memberInfo.GetFieldName(namingConvention));

        if (value == null)
        {
            switch (comparison)
            {
                case ExpressionType.Equal:
                    return $"{fieldName} IS NULL";
                case ExpressionType.NotEqual:
                    return $"{fieldName} IS NOT NULL";
            }
        }

        var parameterName = UniqueParameter(memberInfo, adhesive);
        adhesive.Parameters.Add($"{parameterName}", value);
        return $"{fieldName} {comparison.ToComparisonSymbol(reverse)} {adhesive.SqlAdapter.FormatSqlParameter(parameterName)}";
    }

    public static string BuildLikeOrEqualCondition(ParameterExpression parameterExpression, MethodCallExpression methodCallExpression, WhereClauseAdhesive adhesive, NamingConvention namingConvention, bool reverse)
    {
        if (methodCallExpression.Object is MemberExpression { Expression: ParameterExpression parameterExpression2 } memberExpression
            && parameterExpression2.Name == parameterExpression.Name)
        {
            string symbol;
            string valueSymbol;
            switch (methodCallExpression.Method.Name)
            {
                case "Equals":
                    symbol = !reverse ? "= {0}" : "<> {0}";
                    valueSymbol = "{0}";
                    break;
                case "StartsWith":
                    symbol = !reverse ? "LIKE {0}" : "NOT LIKE {0}";
                    valueSymbol = "{0}%";
                    break;
                case "EndsWith":
                    symbol = !reverse ? "LIKE {0}" : "NOT LIKE {0}";
                    valueSymbol = "%{0}";
                    break;
                case "Contains":
                    symbol = !reverse ? "LIKE {0}" : "NOT LIKE {0}";
                    valueSymbol = "%{0}%";
                    break;
                default:
                    throw new NotSupportedException($"Unsupported MethodCallExpression Method: {methodCallExpression.Method.Name}");
            }

            var memberInfo = memberExpression.Member;
            var fieldName = adhesive.SqlAdapter.FormatFieldName(memberInfo.GetFieldName(namingConvention));
            var parameterName = UniqueParameter(memberInfo, adhesive);
            var value = ConstantExtractor.ParseConstant(methodCallExpression.Arguments[0]);
            adhesive.Parameters.Add($"{parameterName}", string.Format(valueSymbol, value));
            return string.Format($"{fieldName} {symbol}", $"{adhesive.SqlAdapter.FormatSqlParameter(parameterName)}");
        }

        throw new NotSupportedException($"Unsupported MethodCallExpression: {methodCallExpression}");
    }

    public static string BuildInCondition(ParameterExpression parameterExpression, MemberExpression memberExpression, Expression valueExpression, WhereClauseAdhesive adhesive, NamingConvention namingConvention, bool reverse)
    {
        if (memberExpression.Expression is not ParameterExpression parameterExpression2
            || parameterExpression2.Name != parameterExpression.Name)
        {
            throw new NotSupportedException($"Unsupported MemberExpression: {memberExpression}");
        }

        var memberInfo = memberExpression.Member;
        var fieldName = adhesive.SqlAdapter.FormatFieldName(memberInfo.GetFieldName(namingConvention));
        var parameterName = UniqueParameter(memberInfo, adhesive);
        var value = ConstantExtractor.ParseConstant(valueExpression);
        adhesive.Parameters.Add($"{parameterName}", value);
        return $"{fieldName} {(!reverse ? "IN" : "NOT IN")} {adhesive.SqlAdapter.FormatSqlParameter(parameterName)}";
    }

    public static string BuildIsNullOrEmptyCondition(ParameterExpression parameterExpression, MethodCallExpression methodCallExpression, WhereClauseAdhesive adhesive, NamingConvention namingConvention, bool reverse)
    {
        if (methodCallExpression.Arguments[0] is MemberExpression { Expression: ParameterExpression parameterExpression2 } memberExpression
            && parameterExpression2.Name == parameterExpression.Name)
        {
            var memberInfo = memberExpression.Member;
            var fieldName = adhesive.SqlAdapter.FormatFieldName(memberInfo.GetFieldName(namingConvention));
            return !reverse
                    ? $"({fieldName} IS NULL OR {fieldName} = '')"
                    : $"{fieldName} IS NOT NULL AND {fieldName} <> ''";
        }

        throw new NotSupportedException($"Unsupported MethodCallExpression: {methodCallExpression}");
    }

    public static string UniqueParameter(MemberInfo mi, WhereClauseAdhesive adhesive)
    {
        return UniqueParameter(mi.Name, adhesive.Parameters);
    }

    public static string UniqueParameter(string paramName, IDictionary<string, object> parameterDic)
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

    private static string ToComparisonSymbol(this ExpressionType expressionType, bool reverse = false)
    {
        switch (expressionType)
        {
            case ExpressionType.Equal:
                return "=";
            case ExpressionType.NotEqual:
                return "<>";
            case ExpressionType.GreaterThan:
                return !reverse ? ">" : "<";
            case ExpressionType.GreaterThanOrEqual:
                return !reverse ? ">=" : "<=";
            case ExpressionType.LessThan:
                return !reverse ? "<" : ">";
            case ExpressionType.LessThanOrEqual:
                return !reverse ? "<=" : ">=";
            default:
                throw new NotSupportedException($"Unsupported ExpressionType: {expressionType}");
        }
    }
}