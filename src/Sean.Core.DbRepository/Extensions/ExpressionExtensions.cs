using System;
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
            var expressionBody = fieldExpression.Body;
            string memberName = null;
            if (expressionBody is UnaryExpression unaryExpression)
            {
                if (unaryExpression.Operand is MemberExpression memberExpression)
                {
                    memberName = memberExpression.Member.Name;
                }
            }
            else if (expressionBody is MemberExpression memberExpression)
            {
                memberName = memberExpression.Member.Name;
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentException("Unsupported Expression.", nameof(fieldExpression));
            }

            return memberName;
        }
    }
}
