using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Sean.Core.DbRepository.Extensions
{
    public static class ExpressionExtensions
    {
        public static string GetMemberName<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> fieldExpression)
        {
            return fieldExpression.Body.GetMemberName();
        }

        public static string GetMemberName(this Expression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            string memberName = null;
            if (expression is UnaryExpression unaryExpression)
            {
                if (unaryExpression.Operand is MemberExpression memberExpression)
                {
                    memberName = memberExpression.Member.Name;
                }
            }
            else if (expression is MemberExpression memberExpression)
            {
                memberName = memberExpression.Member.Name;
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentException("Unsupported Expression.", nameof(expression));
            }

            return memberName;
        }

        public static List<string> GetMemberNames(this Expression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var result = new List<string>();
            //var expressionType = expression.NodeType;
            //if (expression.Type.IsArray || typeof(IEnumerable).IsAssignableFrom(expression.Type))
            if (expression is NewArrayExpression newArrayExpression)// 数组
            {
                foreach (var subExpression in newArrayExpression.Expressions)
                {
                    var memberName = subExpression.GetMemberName();
                    if (!result.Contains(memberName))
                    {
                        result.Add(memberName);
                    }
                }
            }
            else if (expression is ListInitExpression listInitExpression)// List泛型集合
            {
                foreach (var initializer in listInitExpression.Initializers)
                {
                    foreach (var argument in initializer.Arguments)
                    {
                        var memberName = argument.GetMemberName();
                        if (!result.Contains(memberName))
                        {
                            result.Add(memberName);
                        }
                    }
                }
            }
            else if (expression is NewExpression newExpression)// 匿名类型
            {
                foreach (var memberInfo in newExpression.Members)
                {
                    var memberName = memberInfo.Name;
                    if (!result.Contains(memberName))
                    {
                        result.Add(memberName);
                    }
                }
            }
            else
            {
                var memberName = expression.GetMemberName();
                if (!result.Contains(memberName))
                {
                    result.Add(memberName);
                }
            }

            if (!result.Any())
            {
                throw new ArgumentException("Unsupported Expression.", nameof(expression));
            }

            return result;
        }
    }
}
