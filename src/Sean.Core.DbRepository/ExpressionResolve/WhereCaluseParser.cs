using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Sean.Core.DbRepository
{
    public static class WhereCaluseParser
    {
        public static StringBuilder Parse(Expression expression, WhereClauseAdhesive adhesive)
        {
            if (expression is BinaryExpression binaryExpression)
            {
                if (IsLogicType(binaryExpression.NodeType))
                {
                    StringBuilder sqlBuilder = new StringBuilder();
                    var leftClause = Parse(binaryExpression.Left, adhesive);
                    sqlBuilder.Append($"({leftClause})");
                    sqlBuilder.Append($" {binaryExpression.NodeType.ToLogicSymbol()} ");
                    var rightClause = Parse(binaryExpression.Right, adhesive);
                    sqlBuilder.Append($"({rightClause})");

                    return sqlBuilder;
                }
                else if (binaryExpression.Left is UnaryExpression convertExpression
                    && convertExpression.NodeType == ExpressionType.Convert
                    && convertExpression.Operand.Type.IsEnum
                    && convertExpression.Operand is MemberExpression enumMemberExpression
                    && IsDataComparator(binaryExpression.NodeType))
                {
                    //Support the enum Property, For example: u.UserType == UserType.Admin
                    return ConditionBuilder.BuildCondition(enumMemberExpression.Member, adhesive, binaryExpression.NodeType, ConstantExtractor.ParseConstant(binaryExpression.Right));
                }
                else if (binaryExpression.Left is MemberExpression memberExpression && IsDataComparator(binaryExpression.NodeType))
                {
                    //Basic case, For example: u.Age > 18
                    return ConditionBuilder.BuildCondition(memberExpression.Member, adhesive, binaryExpression.NodeType, ConstantExtractor.ParseConstant(binaryExpression.Right));
                }
                else
                {
                    throw new NotSupportedException($"Unknow Left:{binaryExpression.Left.GetType()} Right:{binaryExpression.Right.GetType()} NodeType:{binaryExpression.NodeType}");
                }
            }
            else if (expression is MethodCallExpression methodCallExpression)
            {
                if (methodCallExpression.Method.DeclaringType == typeof(string)
                    && (methodCallExpression.Method.Name == nameof(string.Contains)
                      || methodCallExpression.Method.Name == nameof(string.StartsWith)
                      || methodCallExpression.Method.Name == nameof(string.EndsWith))
                      || methodCallExpression.Method.Name == nameof(object.Equals))
                {
                    //"Like" condition for string property, For example: u.Name.Contains("A")
                    return ConditionBuilder.BuildLikeOrEqualCondition(methodCallExpression, adhesive);
                }
                else if (methodCallExpression.Method.DeclaringType == typeof(string)
                         && methodCallExpression.Method.Name == nameof(string.IsNullOrEmpty))
                {
                    // example: entity => string.IsNullOrEmpty(entity.Email)
                    return ConditionBuilder.BuildIsNullOrEmptyCondition(methodCallExpression, adhesive);
                }
                else if (methodCallExpression.Method.Name == "Contains")
                {
                    if (methodCallExpression.Method.DeclaringType == typeof(Enumerable)
                        && methodCallExpression.Arguments.Count == 2)
                    {
                        //"In" Condition, Support the `Contains` Method of IEnumerable<T> type
                        //For example: string[] values = new string[]{ "foo", "bar"};
                        //             values.Contains(u.Name)
                        return ConditionBuilder.BuildInCondition(methodCallExpression.Arguments[1] as MemberExpression, methodCallExpression.Arguments[0], adhesive);
                    }
                    else if (methodCallExpression.Method.DeclaringType != null
                             && (methodCallExpression.Method.DeclaringType.IsGenericType
                                 && methodCallExpression.Method.DeclaringType.GetGenericTypeDefinition() == typeof(ICollection<>)
                                 || methodCallExpression.Method.DeclaringType.GetInterfaces().Where(c => c.IsGenericType).Any(c => c.GetGenericTypeDefinition() == typeof(ICollection<>)))
                             && methodCallExpression.Arguments.Count == 1)
                    {
                        //"In" condition, Support the `Contains` extension Method of ICollection<TSource> Type
                        //For example: List<string> values = new List<string> { "foo", "bar"};
                        //             values.Contains(u.Name)
                        return ConditionBuilder.BuildInCondition(methodCallExpression.Arguments[0] as MemberExpression, methodCallExpression.Object, adhesive);
                    }
                }
            }
            else if (expression is MemberExpression trueMemberExpression && trueMemberExpression.Type == typeof(bool))
            {
                if (trueMemberExpression.Member.Name == "HasValue"
                    && trueMemberExpression.Member.DeclaringType != null
                    && trueMemberExpression.Member.DeclaringType.IsGenericType
                    && trueMemberExpression.Member.DeclaringType.GetGenericTypeDefinition() == typeof(Nullable<>)
                    && trueMemberExpression.Expression is MemberExpression memberExpression)
                {
                    // example: Nullable<>.HasValue
                    return ConditionBuilder.BuildCondition(memberExpression.Member, adhesive, ExpressionType.NotEqual, null);
                }

                //Support bool type property, For example: u.Sex
                return ConditionBuilder.BuildCondition(trueMemberExpression.Member, adhesive, ExpressionType.Equal, true);
            }
            else if (expression is UnaryExpression unaryExpression
                && unaryExpression.NodeType == ExpressionType.Not
                && unaryExpression.Type == typeof(bool))
            {
                if (unaryExpression.Operand is MemberExpression falseMemberExpression)
                {
                    if (falseMemberExpression.Member.Name == "HasValue"
                        && falseMemberExpression.Member.DeclaringType != null
                        && falseMemberExpression.Member.DeclaringType.IsGenericType
                        && falseMemberExpression.Member.DeclaringType.GetGenericTypeDefinition() == typeof(Nullable<>)
                        && falseMemberExpression.Expression is MemberExpression memberExpression)
                    {
                        // example: Nullable<>.HasValue
                        return ConditionBuilder.BuildCondition(memberExpression.Member, adhesive, ExpressionType.Equal,
                            null);
                    }

                    //Support bool type property, For example: !u.Sex
                    return ConditionBuilder.BuildCondition(falseMemberExpression.Member, adhesive, ExpressionType.Equal,
                        false);
                }
                else if (unaryExpression.Operand is MethodCallExpression falseMethodCallExpression)
                {
                    if (falseMethodCallExpression.Method.DeclaringType == typeof(string)
                        && falseMethodCallExpression.Method.Name == nameof(string.IsNullOrEmpty))
                    {
                        // example: entity => !string.IsNullOrEmpty(entity.Email)
                        return ConditionBuilder.BuildIsNullOrEmptyCondition(falseMethodCallExpression, adhesive, true);
                    }
                    else if (falseMethodCallExpression.Method.Name == nameof(object.Equals))
                    {
                        // example: entity => !entity.UserName.Equals(userName)
                        return ConditionBuilder.BuildLikeOrEqualCondition(falseMethodCallExpression, adhesive, true);
                    }
                }
            }
            else if (expression is ConstantExpression constantExpression)
            {
                if (constantExpression.Type == typeof(bool))
                {
                    // example1: entity => true
                    // example2: entity => false
                    StringBuilder sqlBuilder = new StringBuilder();
                    var value = (bool)constantExpression.Value;
                    sqlBuilder.Append(value ? "1=1" : "1=2");
                    return sqlBuilder;
                }
            }

            throw new NotSupportedException($"Unsupported expression type: {expression.GetType()}");
        }

        private static bool IsDataComparator(ExpressionType expressionType)
        {
            switch (expressionType)
            {
                case ExpressionType.Equal:
                case ExpressionType.LessThan:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.NotEqual:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsLogicType(ExpressionType expressionType)
        {
            switch (expressionType)
            {
                case ExpressionType.OrElse:
                case ExpressionType.AndAlso:
                    return true;
                default:
                    return false;
            }
        }

        private static string ToLogicSymbol(this ExpressionType expressionType)
        {
            switch (expressionType)
            {
                case ExpressionType.AndAlso:
                    return "AND";
                case ExpressionType.OrElse:
                    return "OR";
                default:
                    throw new NotSupportedException($"Unknown ExpressionType {expressionType}");
            }
        }
    }
}
