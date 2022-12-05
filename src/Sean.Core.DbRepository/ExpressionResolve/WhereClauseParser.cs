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
        if (expression is BinaryExpression binaryExpression)
        {
            if (IsLogicalOperation(binaryExpression.NodeType))
            {
                var sqlBuilder = new StringBuilder();
                var leftClause = Parse(parameterExpression, binaryExpression.Left, adhesive);
                var rightClause = Parse(parameterExpression, binaryExpression.Right, adhesive);
                //sqlBuilder.Append($"({leftClause}) {binaryExpression.NodeType.ToSqlString()} ({rightClause})");
                switch (binaryExpression.NodeType)
                {
                    case ExpressionType.AndAlso:
                        sqlBuilder.Append($"{leftClause} AND {rightClause}");
                        break;
                    case ExpressionType.OrElse:
                        sqlBuilder.Append($"({leftClause} OR {rightClause})");
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported BinaryExpression NodeType: {binaryExpression.NodeType}");
                }
                return sqlBuilder;
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

        if (expression is MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.DeclaringType == typeof(string)
                && methodCallExpression.Method.Name is nameof(string.Contains) or nameof(string.StartsWith) or nameof(string.EndsWith) or nameof(object.Equals)
                && methodCallExpression.Object is MemberExpression { Expression: ParameterExpression parameterExpression4 }
                && parameterExpression4.Name == parameterExpression.Name)
            {
                //"Like" condition for string property, Code example: entity.Name.Contains("A")
                return ConditionBuilder.BuildLikeOrEqualCondition(parameterExpression, methodCallExpression, adhesive);
            }
            else if (methodCallExpression.Method.DeclaringType == typeof(string)
                     && methodCallExpression.Method.Name == nameof(string.IsNullOrEmpty)
                     && methodCallExpression.Arguments[0] is MemberExpression { Expression: ParameterExpression parameterExpression5 }
                     && parameterExpression5.Name == parameterExpression.Name)
            {
                // Code example: entity => string.IsNullOrEmpty(entity.Email)
                return ConditionBuilder.BuildIsNullOrEmptyCondition(parameterExpression, methodCallExpression, adhesive);
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

        if (expression is MemberExpression memberExpression)
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

        if (expression is UnaryExpression unaryExpression)
        {
            if (unaryExpression.NodeType == ExpressionType.Not && unaryExpression.Type == typeof(bool))
            {
                if (unaryExpression.Operand is MemberExpression memberExpression2)
                {
                    if (memberExpression2.Member.Name == "HasValue"
                        && memberExpression2.Member.DeclaringType != null
                        && memberExpression2.Member.DeclaringType.IsGenericType
                        && memberExpression2.Member.DeclaringType.GetGenericTypeDefinition() == typeof(Nullable<>)
                        && memberExpression2.Expression is MemberExpression { Expression: ParameterExpression parameterExpression2 } memberExpression3
                        && parameterExpression2.Name == parameterExpression.Name)
                    {
                        // Code example: Nullable<>.HasValue
                        return ConditionBuilder.BuildCondition(parameterExpression, memberExpression3, adhesive, ExpressionType.Equal, null);
                    }

                    if (memberExpression2.Expression is ParameterExpression parameterExpression3
                        && parameterExpression3.Name == parameterExpression.Name)
                    {
                        // Support bool type property, Code example: !entity.Sex
                        return ConditionBuilder.BuildCondition(parameterExpression, memberExpression2, adhesive, ExpressionType.Equal, false);
                    }
                }
                else if (unaryExpression.Operand is MethodCallExpression falseMethodCallExpression)
                {
                    if (falseMethodCallExpression.Method.DeclaringType == typeof(string)
                        && falseMethodCallExpression.Method.Name == nameof(string.IsNullOrEmpty)
                        && falseMethodCallExpression.Arguments[0] is MemberExpression { Expression: ParameterExpression parameterExpression3 }
                        && parameterExpression3.Name == parameterExpression.Name)
                    {
                        // Code example: entity => !string.IsNullOrEmpty(entity.Email)
                        return ConditionBuilder.BuildIsNullOrEmptyCondition(parameterExpression, falseMethodCallExpression, adhesive, true);
                    }
                    else if (falseMethodCallExpression.Method.Name == nameof(object.Equals)
                             && falseMethodCallExpression.Object is MemberExpression { Expression: ParameterExpression parameterExpression2 }
                             && parameterExpression2.Name == parameterExpression.Name)
                    {
                        // Code example: entity => !entity.UserName.Equals(userName)
                        return ConditionBuilder.BuildLikeOrEqualCondition(parameterExpression, falseMethodCallExpression, adhesive, true);
                    }
                }
            }

            throw new NotSupportedException($"Unsupported UnaryExpression: {unaryExpression}");
        }

        if (expression is ConstantExpression constantExpression)
        {
            if (constantExpression.Type == typeof(bool))
            {
                // Code example:
                // entity => true
                // entity => false
                StringBuilder sqlBuilder = new StringBuilder();
                var value = (bool)constantExpression.Value;
                sqlBuilder.Append(value ? "1=1" : "1=2");
                return sqlBuilder;
            }

            throw new NotSupportedException($"Unsupported ConstantExpression: {constantExpression}");
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