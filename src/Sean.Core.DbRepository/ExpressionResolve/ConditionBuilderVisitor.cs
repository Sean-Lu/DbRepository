using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository
{
    public class ConditionBuilderVisitor : ExpressionVisitor
    {
        private readonly Stack<string> _stringStack = new();

        public string GetCondition()
        {
            return string.Concat(_stringStack.ToArray());
        }

        protected override Expression VisitBinary(BinaryExpression binaryExpression)
        {
            if (binaryExpression == null) throw new ArgumentNullException(nameof(binaryExpression));

            _stringStack.Push(")");
            base.Visit(binaryExpression.Right);
            _stringStack.Push($" {binaryExpression.NodeType.ToSqlString()} ");
            base.Visit(binaryExpression.Left);
            _stringStack.Push("(");

            return binaryExpression;
        }

        protected override Expression VisitMember(MemberExpression memberExpression)
        {
            if (memberExpression == null) throw new ArgumentNullException(nameof(memberExpression));

            if (memberExpression.Expression is ConstantExpression)
            {
                var value2 = ConstantExtractor.ParseConstant(memberExpression);
                _stringStack.Push($"'{value2}'");
            }
            else
            {
                _stringStack.Push($"[{memberExpression.Member.Name}]");
            }
            return memberExpression;
        }

        protected override Expression VisitConstant(ConstantExpression constantExpression)
        {
            if (constantExpression == null) throw new ArgumentNullException(nameof(constantExpression));

            _stringStack.Push(constantExpression.Value.ToString());
            return constantExpression;
        }

        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression == null) throw new ArgumentNullException("MethodCallExpression");

            string format;
            switch (methodCallExpression.Method.Name)
            {
                case "StartsWith":
                    format = "({0} LIKE '{1}%')";
                    break;
                case "EndsWith":
                    format = "({0} LIKE '%{1}')";
                    break;
                case "Contains":
                    format = "({0} LIKE '%{1}%')";
                    break;
                default:
                    throw new NotImplementedException($"[{methodCallExpression.NodeType}]Unsupported method: {methodCallExpression.Method.Name}.");
            }
            base.Visit(methodCallExpression.Object);
            base.Visit(methodCallExpression.Arguments[0]);
            string right = _stringStack.Pop();
            string left = _stringStack.Pop();
            _stringStack.Push(string.Format(format, left, right));
            return methodCallExpression;
        }
    }
}
