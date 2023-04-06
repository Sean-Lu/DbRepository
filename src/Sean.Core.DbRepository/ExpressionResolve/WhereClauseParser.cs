using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Sean.Core.DbRepository;

public static class WhereClauseParser
{
    public static StringBuilder Parse<TEntity>(Expression<Func<TEntity, bool>> whereExpression, WhereClauseAdhesive adhesive)
    {
        return Parse(whereExpression.Parameters.FirstOrDefault(), whereExpression.Body, adhesive);
    }

    private static StringBuilder Parse(ParameterExpression parameterExpression, Expression expression, WhereClauseAdhesive adhesive)
    {
        switch (expression)
        {
            case BinaryExpression binaryExpression:
                {
                    if (IsLogicalOperation(binaryExpression.NodeType))
                    {
                        var sb = new StringBuilder();
                        var leftClause = Parse(parameterExpression, binaryExpression.Left, adhesive);
                        var rightClause = Parse(parameterExpression, binaryExpression.Right, adhesive);
                        //sqlBuilder.Append($"({leftClause}) {binaryExpression.NodeType.ToSqlString()} ({rightClause})");
                        switch (binaryExpression.NodeType)
                        {
                            case ExpressionType.AndAlso:
                                sb.Append($"{leftClause} AND {rightClause}");
                                break;
                            case ExpressionType.OrElse:
                                sb.Append($"({leftClause} OR {rightClause})");
                                break;
                            default:
                                throw new NotSupportedException($"Unsupported BinaryExpression NodeType: {binaryExpression.NodeType}");
                        }
                        return sb;
                    }
                    else if (IsComparativeOperation(binaryExpression.NodeType))
                    {
                        if (binaryExpression.Left is UnaryExpression { NodeType: ExpressionType.Convert, Operand: MemberExpression { Expression: ParameterExpression parameterExpression2 } convertMemberExpression }
                            && parameterExpression2.Name == parameterExpression.Name)
                        {
                            // Code example: entity.UserType == UserType.Admin
                            return ConditionBuilder.BuildCondition(parameterExpression, convertMemberExpression, adhesive, binaryExpression.NodeType, ConstantExtractor.ParseConstant(binaryExpression.Right));
                        }
                        else if (binaryExpression.Right is UnaryExpression { NodeType: ExpressionType.Convert, Operand: MemberExpression { Expression: ParameterExpression parameterExpression3 } convertMemberExpression2 }
                                 && parameterExpression3.Name == parameterExpression.Name)
                        {
                            // Code example: UserType.Admin == entity.UserType
                            return ConditionBuilder.BuildCondition(parameterExpression, convertMemberExpression2, adhesive, binaryExpression.NodeType, ConstantExtractor.ParseConstant(binaryExpression.Left), true);
                        }
                        else if (binaryExpression.Left is MemberExpression { Expression: ParameterExpression parameterExpression4 } memberExpression2
                                 && parameterExpression4.Name == parameterExpression.Name)
                        {
                            // Code example: entity.Age > 18
                            return ConditionBuilder.BuildCondition(parameterExpression, memberExpression2, adhesive, binaryExpression.NodeType, ConstantExtractor.ParseConstant(binaryExpression.Right));
                        }
                        else if (binaryExpression.Right is MemberExpression { Expression: ParameterExpression parameterExpression5 } memberExpression3
                                 && parameterExpression5.Name == parameterExpression.Name)
                        {
                            // Code example: 18 < entity.Age
                            return ConditionBuilder.BuildCondition(parameterExpression, memberExpression3, adhesive, binaryExpression.NodeType, ConstantExtractor.ParseConstant(binaryExpression.Left), true);
                        }
                    }

                    throw new NotSupportedException($"Unsupported BinaryExpression: {binaryExpression}");
                }
            case MethodCallExpression methodCallExpression:
                {
                    if (methodCallExpression.Method.DeclaringType == typeof(string))
                    {
                        switch (methodCallExpression.Method.Name)
                        {
                            case nameof(string.IsNullOrEmpty) when methodCallExpression.Arguments[0] is MemberExpression { Expression: ParameterExpression parameterExpression5 } && parameterExpression5.Name == parameterExpression.Name:
                                // Code example: entity => string.IsNullOrEmpty(entity.Email)
                                return ConditionBuilder.BuildIsNullOrEmptyCondition(parameterExpression, methodCallExpression, adhesive);
                            case nameof(string.Contains) or nameof(string.StartsWith) or nameof(string.EndsWith) or nameof(object.Equals) when methodCallExpression.Object is MemberExpression { Expression: ParameterExpression parameterExpression4 } && parameterExpression4.Name == parameterExpression.Name:
                                // Code example: entity.Name.Contains("A")
                                return ConditionBuilder.BuildLikeOrEqualCondition(parameterExpression, methodCallExpression, adhesive, false);
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
                            //             values.Contains(entity.Name)
                            return ConditionBuilder.BuildInCondition(parameterExpression, memberExpression2, methodCallExpression.Arguments[0], adhesive);
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
                            // Code example: List<string> values = new List<string> { "foo", "bar"};
                            //             values.Contains(entity.Name)
                            return ConditionBuilder.BuildInCondition(parameterExpression, memberExpression3, methodCallExpression.Object, adhesive);
                        }
                    }

                    throw new NotSupportedException($"Unsupported MethodCallExpression: {methodCallExpression}");
                }
            case MemberExpression memberExpression:
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
                            // Code example: Nullable<>.HasValue
                            return ConditionBuilder.BuildCondition(parameterExpression, memberExpression2, adhesive, ExpressionType.NotEqual, null);
                        }

                        if (memberExpression.Expression is ParameterExpression parameterExpression3
                            && parameterExpression3.Name == parameterExpression.Name)
                        {
                            // Support bool type property, Code example: entity.Sex
                            return ConditionBuilder.BuildCondition(parameterExpression, memberExpression, adhesive, ExpressionType.Equal, true);
                        }
                    }

                    throw new NotSupportedException($"Unsupported MemberExpression: {memberExpression}");
                }
            case UnaryExpression unaryExpression:
                {
                    switch (unaryExpression.NodeType)
                    {
                        case ExpressionType.Not when unaryExpression.Type == typeof(bool):
                            {
                                switch (unaryExpression.Operand)
                                {
                                    case MemberExpression memberExpression2 when memberExpression2.Member.Name == "HasValue"
                                                                                 && memberExpression2.Member.DeclaringType != null
                                                                                 && memberExpression2.Member.DeclaringType.IsGenericType
                                                                                 && memberExpression2.Member.DeclaringType.GetGenericTypeDefinition() == typeof(Nullable<>)
                                                                                 && memberExpression2.Expression is MemberExpression { Expression: ParameterExpression parameterExpression2 } memberExpression3
                                                                                 && parameterExpression2.Name == parameterExpression.Name:
                                        // Code example: Nullable<>.HasValue
                                        return ConditionBuilder.BuildCondition(parameterExpression, memberExpression3, adhesive, ExpressionType.Equal, null);
                                    case MemberExpression memberExpression2 when memberExpression2.Expression is ParameterExpression parameterExpression3
                                                                                 && parameterExpression3.Name == parameterExpression.Name:
                                        // Code example: !entity.Sex
                                        return ConditionBuilder.BuildCondition(parameterExpression, memberExpression2, adhesive, ExpressionType.Equal, false);
                                    case MethodCallExpression falseMethodCallExpression:
                                        {
                                            if (falseMethodCallExpression.Method.DeclaringType == typeof(string))
                                            {
                                                switch (falseMethodCallExpression.Method.Name)
                                                {
                                                    case nameof(string.IsNullOrEmpty) when falseMethodCallExpression.Arguments[0] is MemberExpression { Expression: ParameterExpression parameterExpression3 } && parameterExpression3.Name == parameterExpression.Name:
                                                        // Code example: entity => !string.IsNullOrEmpty(entity.Email)
                                                        return ConditionBuilder.BuildIsNullOrEmptyCondition(parameterExpression, falseMethodCallExpression, adhesive, true);
                                                    case nameof(string.Contains) or nameof(string.StartsWith) or nameof(string.EndsWith) or nameof(object.Equals) when falseMethodCallExpression.Object is MemberExpression { Expression: ParameterExpression parameterExpression2 } && parameterExpression2.Name == parameterExpression.Name:
                                                        // Code example: entity => !entity.UserName.Equals(userName)
                                                        return ConditionBuilder.BuildLikeOrEqualCondition(parameterExpression, falseMethodCallExpression, adhesive, true);
                                                }
                                            }
                                            break;
                                        }
                                }
                                break;
                            }
                    }

                    throw new NotSupportedException($"Unsupported UnaryExpression: {unaryExpression}");
                }
            case ConstantExpression constantExpression:
                {
                    if (constantExpression.Type == typeof(bool))
                    {
                        // Code example:
                        // entity => true
                        // entity => false
                        var sb = new StringBuilder();
                        var value = (bool)constantExpression.Value;
                        sb.Append(value ? "1=1" : "1=2");
                        return sb;
                    }

                    throw new NotSupportedException($"Unsupported ConstantExpression: {constantExpression}");
                }
        }

        throw new NotSupportedException($"Unsupported Expression: {expression}");
    }

    /// <summary>
    /// 逻辑运算
    /// </summary>
    /// <param name="expressionType"></param>
    /// <returns></returns>
    private static bool IsLogicalOperation(ExpressionType expressionType)
    {
        return expressionType is ExpressionType.OrElse or ExpressionType.AndAlso;
    }

    /// <summary>
    /// 比较运算
    /// </summary>
    /// <param name="expressionType"></param>
    /// <returns></returns>
    private static bool IsComparativeOperation(ExpressionType expressionType)
    {
        return expressionType is ExpressionType.Equal
            or ExpressionType.NotEqual
            or ExpressionType.LessThan
            or ExpressionType.LessThanOrEqual
            or ExpressionType.GreaterThan
            or ExpressionType.GreaterThanOrEqual;
    }
}