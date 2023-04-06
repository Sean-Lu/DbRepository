using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository;

internal static class ConditionBuilder
{
    public static StringBuilder BuildCondition(ParameterExpression parameterExpression, MemberExpression memberExpression, WhereClauseAdhesive adhesive, ExpressionType comparison, object value, bool comparisonReverse = false)
    {
        if (memberExpression.Expression is not ParameterExpression parameterExpression2
            || parameterExpression2.Name != parameterExpression.Name)
        {
            throw new NotSupportedException($"Unsupported MemberExpression: {memberExpression}");
        }

        var memberInfo = memberExpression.Member;
        var fieldName = adhesive.SqlAdapter.FormatFieldName(memberInfo.GetFieldName());

        if (value == null)
        {
            switch (comparison)
            {
                case ExpressionType.Equal:
                    return new StringBuilder($"{fieldName} is null");
                case ExpressionType.NotEqual:
                    return new StringBuilder($"{fieldName} is not null");
            }
        }

        var parameterName = UniqueParameter(memberInfo, adhesive);
        adhesive.Parameters.Add($"{parameterName}", value);
        return new StringBuilder($"{fieldName} {comparison.ToComparisonSymbol(comparisonReverse)} {adhesive.SqlAdapter.FormatInputParameter(parameterName)}");
    }

    public static StringBuilder BuildLikeOrEqualCondition(ParameterExpression parameterExpression, MethodCallExpression methodCallExpression, WhereClauseAdhesive adhesive, bool notEquals)
    {
        if (methodCallExpression.Object is MemberExpression { Expression: ParameterExpression parameterExpression2 } memberExpression
            && parameterExpression2.Name == parameterExpression.Name)
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
                    symbol = notEquals ? "NOT LIKE {0}" : "LIKE {0}";
                    valueSymbol = "{0}%";
                    break;
                case "EndsWith":
                    symbol = notEquals ? "NOT LIKE {0}" : "LIKE {0}";
                    valueSymbol = "%{0}";
                    break;
                case "Contains":
                    symbol = notEquals ? "NOT LIKE {0}" : "LIKE {0}";
                    valueSymbol = "%{0}%";
                    break;
                default:
                    throw new NotSupportedException($"Unsupported MethodCallExpression Method: {methodCallExpression.Method.Name}");
            }

            var memberInfo = memberExpression.Member;
            var fieldName = adhesive.SqlAdapter.FormatFieldName(memberInfo.GetFieldName());
            var parameterName = UniqueParameter(memberInfo, adhesive);
            var value = ConstantExtractor.ParseConstant(methodCallExpression.Arguments[0]);
            adhesive.Parameters.Add($"{parameterName}", string.Format(valueSymbol, value));
            return new StringBuilder(string.Format($"{fieldName} {symbol}", $"{adhesive.SqlAdapter.FormatInputParameter(parameterName)}"));
        }

        throw new NotSupportedException($"Unsupported MethodCallExpression: {methodCallExpression}");
    }

    public static StringBuilder BuildInCondition(ParameterExpression parameterExpression, MemberExpression memberExpression, Expression valueExpression, WhereClauseAdhesive adhesive)
    {
        if (memberExpression.Expression is not ParameterExpression parameterExpression2
            || parameterExpression2.Name != parameterExpression.Name)
        {
            throw new NotSupportedException($"Unsupported MemberExpression: {memberExpression}");
        }

        var memberInfo = memberExpression.Member;
        var fieldName = adhesive.SqlAdapter.FormatFieldName(memberInfo.GetFieldName());
        var parameterName = UniqueParameter(memberInfo, adhesive);
        var value = ConstantExtractor.ParseConstant(valueExpression);
        adhesive.Parameters.Add($"{parameterName}", value);
        return new StringBuilder($"{fieldName} IN {adhesive.SqlAdapter.FormatInputParameter(parameterName)}");
    }

    public static StringBuilder BuildIsNullOrEmptyCondition(ParameterExpression parameterExpression, MethodCallExpression methodCallExpression, WhereClauseAdhesive adhesive, bool reverse = false)
    {
        if (methodCallExpression.Arguments[0] is MemberExpression { Expression: ParameterExpression parameterExpression2 } memberExpression
            && parameterExpression2.Name == parameterExpression.Name)
        {
            var memberInfo = memberExpression.Member;
            var fieldName = adhesive.SqlAdapter.FormatFieldName(memberInfo.GetFieldName());
            return reverse
                ? new StringBuilder($"{fieldName} is not null AND {fieldName} <> ''")
                : new StringBuilder($"({fieldName} is null OR {fieldName} = '')");
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

    private static string ToComparisonSymbol(this ExpressionType expressionType, bool comparisonReverse = false)
    {
        switch (expressionType)
        {
            case ExpressionType.Equal:
                return "=";
            case ExpressionType.NotEqual:
                return "<>";
            case ExpressionType.GreaterThan:
                return !comparisonReverse ? ">" : "<";
            case ExpressionType.GreaterThanOrEqual:
                return !comparisonReverse ? ">=" : "<=";
            case ExpressionType.LessThan:
                return !comparisonReverse ? "<" : ">";
            case ExpressionType.LessThanOrEqual:
                return !comparisonReverse ? "<=" : ">=";
            default:
                throw new NotSupportedException($"Unsupported ExpressionType: {expressionType}");
        }
    }
}