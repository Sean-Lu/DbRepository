using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository;

public static class WhereClauseParser
{
    public static string Parse<TEntity>(Expression<Func<TEntity, bool>> whereExpression, WhereClauseAdhesive adhesive)
    {
        ParameterExpression parameterExpression = whereExpression.Parameters.FirstOrDefault();
        Expression expressionBody = whereExpression.Body;
        NamingConvention namingConvention = typeof(TEntity).GetEntityInfo().NamingConvention;
        return Parse(parameterExpression, expressionBody, adhesive, namingConvention);
    }

    private static string Parse(ParameterExpression parameterExpression, Expression expressionBody, WhereClauseAdhesive adhesive, NamingConvention namingConvention)
    {
        switch (expressionBody)
        {
            case BinaryExpression binaryExpression:
                return ParseBinaryExpression(binaryExpression, parameterExpression, adhesive, namingConvention);
            case MethodCallExpression methodCallExpression:
                return ParseMethodCallExpression(methodCallExpression, parameterExpression, adhesive, namingConvention, false);
            case MemberExpression memberExpression:
                return ParseMemberExpression(memberExpression, parameterExpression, adhesive, namingConvention, false);
            case UnaryExpression unaryExpression:
                return ParseUnaryExpression(unaryExpression, parameterExpression, adhesive, namingConvention);
            case ConstantExpression constantExpression:
                return ParseConstantExpression(constantExpression);
        }

        throw new NotSupportedException($"Unsupported Expression: {expressionBody}");
    }

    private static string ParseBinaryExpression(BinaryExpression binaryExpression, ParameterExpression parameterExpression, WhereClauseAdhesive adhesive, NamingConvention namingConvention)
    {
        switch (binaryExpression.NodeType)
        {
            case ExpressionType.AndAlso or ExpressionType.OrElse:// 逻辑运算
                {
                    var leftClause = Parse(parameterExpression, binaryExpression.Left, adhesive, namingConvention);
                    var rightClause = Parse(parameterExpression, binaryExpression.Right, adhesive, namingConvention);
                    switch (binaryExpression.NodeType)
                    {
                        case ExpressionType.AndAlso:
                            return $"{leftClause} AND {rightClause}";
                        case ExpressionType.OrElse:
                            return $"({leftClause} OR {rightClause})";
                        default:
                            throw new NotSupportedException($"Unsupported BinaryExpression NodeType: {binaryExpression.NodeType}");
                    }
                    break;
                }
            case ExpressionType.Equal
                or ExpressionType.NotEqual
                or ExpressionType.LessThan
                or ExpressionType.LessThanOrEqual
                or ExpressionType.GreaterThan
                or ExpressionType.GreaterThanOrEqual:// 比较运算
                {
                    if (binaryExpression.Left is UnaryExpression { NodeType: ExpressionType.Convert, Operand: MemberExpression { Expression: ParameterExpression parameterExpression2 } convertMemberExpression }
                        && parameterExpression2.Name == parameterExpression.Name)
                    {
                        // Code example: entity => entity.UserType == UserType.Admin
                        return ConditionBuilder.BuildCondition(parameterExpression, convertMemberExpression, adhesive, binaryExpression.NodeType, ConstantExtractor.ParseConstant(binaryExpression.Right), namingConvention);
                    }
                    else if (binaryExpression.Right is UnaryExpression { NodeType: ExpressionType.Convert, Operand: MemberExpression { Expression: ParameterExpression parameterExpression3 } convertMemberExpression2 }
                             && parameterExpression3.Name == parameterExpression.Name)
                    {
                        // Code example: entity => UserType.Admin == entity.UserType
                        return ConditionBuilder.BuildCondition(parameterExpression, convertMemberExpression2, adhesive, binaryExpression.NodeType, ConstantExtractor.ParseConstant(binaryExpression.Left), namingConvention, true);
                    }
                    else if (binaryExpression.Left is MemberExpression { Expression: ParameterExpression parameterExpression4 } memberExpression2
                             && parameterExpression4.Name == parameterExpression.Name)
                    {
                        // Code example: entity => entity.Age > 18
                        return ConditionBuilder.BuildCondition(parameterExpression, memberExpression2, adhesive, binaryExpression.NodeType, ConstantExtractor.ParseConstant(binaryExpression.Right), namingConvention);
                    }
                    else if (binaryExpression.Right is MemberExpression { Expression: ParameterExpression parameterExpression5 } memberExpression3
                             && parameterExpression5.Name == parameterExpression.Name)
                    {
                        // Code example: entity => 18 < entity.Age
                        return ConditionBuilder.BuildCondition(parameterExpression, memberExpression3, adhesive, binaryExpression.NodeType, ConstantExtractor.ParseConstant(binaryExpression.Left), namingConvention, true);
                    }
                    break;
                }
        }

        throw new NotSupportedException($"Unsupported BinaryExpression: {binaryExpression}");
    }

    private static string ParseMethodCallExpression(MethodCallExpression methodCallExpression, ParameterExpression parameterExpression, WhereClauseAdhesive adhesive, NamingConvention namingConvention, bool reverse)
    {
        if (methodCallExpression.Method.DeclaringType == typeof(string))
        {
            switch (methodCallExpression.Method.Name)
            {
                case nameof(string.IsNullOrEmpty) when methodCallExpression.Arguments[0] is MemberExpression { Expression: ParameterExpression parameterExpression5 } && parameterExpression5.Name == parameterExpression.Name:
                    // Code example: entity => string.IsNullOrEmpty(entity.Email)
                    //               entity => !string.IsNullOrEmpty(entity.Email)
                    return ConditionBuilder.BuildIsNullOrEmptyCondition(parameterExpression, methodCallExpression, adhesive, namingConvention, reverse);
                case nameof(string.Contains) or nameof(string.StartsWith) or nameof(string.EndsWith) or nameof(string.Equals) when methodCallExpression.Object is MemberExpression { Expression: ParameterExpression parameterExpression4 } && parameterExpression4.Name == parameterExpression.Name:
                    // Code example: entity => entity.UserName.Contains("A")
                    //               entity => entity.UserName.StartsWith("A")
                    //               entity => entity.UserName.EndsWith("A")
                    //               entity => entity.UserName.Equals("A")
                    //               entity => !entity.UserName.Contains("A")
                    //               entity => !entity.UserName.StartsWith("A")
                    //               entity => !entity.UserName.EndsWith("A")
                    //               entity => !entity.UserName.Equals("A")
                    return ConditionBuilder.BuildLikeOrEqualCondition(parameterExpression, methodCallExpression, adhesive, namingConvention, reverse);
            }
        }
        else if (methodCallExpression.Method.Name == "Contains")
        {
            if (methodCallExpression.Method.DeclaringType == typeof(Enumerable)
                && methodCallExpression.Arguments.Count == 2
                && methodCallExpression.Arguments[0] != null
                && methodCallExpression.Arguments[1] is MemberExpression { Expression: ParameterExpression parameterExpression2 } memberExpression2
                && parameterExpression2.Name == parameterExpression.Name)
            {
                //"In" Condition, Support the `Contains` Method of IEnumerable<T> type
                // Code example: string[] values = new string[]{ "foo", "bar"};
                //               entity => values.Contains(entity.Name)
                //               entity => !values.Contains(entity.Name)
                return ConditionBuilder.BuildInCondition(parameterExpression, memberExpression2, methodCallExpression.Arguments[0], adhesive, namingConvention, reverse);
            }
            else if (methodCallExpression.Method.DeclaringType != null
                     && (methodCallExpression.Method.DeclaringType.IsGenericType
                         && methodCallExpression.Method.DeclaringType.GetGenericTypeDefinition() == typeof(ICollection<>)
                         || methodCallExpression.Method.DeclaringType.GetInterfaces().Where(c => c.IsGenericType).Any(c => c.GetGenericTypeDefinition() == typeof(ICollection<>)))
                     && methodCallExpression.Object != null
                     && methodCallExpression.Arguments.Count == 1
                     && methodCallExpression.Arguments[0] is MemberExpression { Expression: ParameterExpression parameterExpression3 } memberExpression3
                     && parameterExpression3.Name == parameterExpression.Name)
            {
                //"In" condition, Support the `Contains` extension Method of ICollection<TSource> Type
                // Code example: List<string> list = new List<string> { "foo", "bar"};
                //               entity => list.Contains(entity.Name)
                //               entity => !list.Contains(entity.Name)
                return ConditionBuilder.BuildInCondition(parameterExpression, memberExpression3, methodCallExpression.Object, adhesive, namingConvention, reverse);
            }
        }

        throw new NotSupportedException($"Unsupported MethodCallExpression: {methodCallExpression}");
    }

    private static string ParseMemberExpression(MemberExpression memberExpression, ParameterExpression parameterExpression, WhereClauseAdhesive adhesive, NamingConvention namingConvention, bool reverse)
    {
        if (memberExpression.Type == typeof(bool))
        {
            if (memberExpression.Member.Name == "HasValue"
                && memberExpression.Member.DeclaringType != null
                && memberExpression.Member.DeclaringType.IsGenericType
                && memberExpression.Member.DeclaringType.GetGenericTypeDefinition() == typeof(Nullable<>)
                && memberExpression.Expression is MemberExpression { Expression: ParameterExpression parameterExpression2 } memberExpression2
                && parameterExpression2.Name == parameterExpression.Name)
            {
                // Code example: entity => entity.NullableField.HasValue
                //               entity => !entity.NullableField.HasValue
                return ConditionBuilder.BuildCondition(parameterExpression, memberExpression2, adhesive, !reverse ? ExpressionType.NotEqual : ExpressionType.Equal, null, namingConvention);
            }

            if (memberExpression.Expression is ParameterExpression parameterExpression3
                && parameterExpression3.Name == parameterExpression.Name)
            {
                // Code example: entity => entity.IsVip
                //               entity => !entity.IsVip
                return ConditionBuilder.BuildCondition(parameterExpression, memberExpression, adhesive, ExpressionType.Equal, !reverse, namingConvention);
            }
        }

        throw new NotSupportedException($"Unsupported MemberExpression: {memberExpression}");
    }

    private static string ParseUnaryExpression(UnaryExpression unaryExpression, ParameterExpression parameterExpression, WhereClauseAdhesive adhesive, NamingConvention namingConvention)
    {
        switch (unaryExpression.NodeType)
        {
            case ExpressionType.Not when unaryExpression.Type == typeof(bool):
                {
                    switch (unaryExpression.Operand)
                    {
                        case MemberExpression memberExpression:
                            return ParseMemberExpression(memberExpression, parameterExpression, adhesive, namingConvention, true);
                        case MethodCallExpression methodCallExpression:
                            return ParseMethodCallExpression(methodCallExpression, parameterExpression, adhesive, namingConvention, true);
                    }
                    break;
                }
        }

        throw new NotSupportedException($"Unsupported UnaryExpression: {unaryExpression}");
    }

    private static string ParseConstantExpression(ConstantExpression constantExpression)
    {
        if (constantExpression.Type == typeof(bool))
        {
            // Code example: entity => true
            //               entity => false
            var value = (bool)constantExpression.Value;
            return value ? "1=1" : "1=2";
        }

        throw new NotSupportedException($"Unsupported ConstantExpression: {constantExpression}");
    }
}