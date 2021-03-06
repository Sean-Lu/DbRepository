using System;
using System.Linq.Expressions;
using System.Reflection;
using Sean.Utility.Format;

namespace Sean.Core.DbRepository
{
    public static class ConstantExtractor
    {
        public static object ParseConstant(Expression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            if (expression is ConstantExpression constantExpression)
            {
                return ParseConstantExpression(constantExpression);
            }
            else if (expression is MemberExpression memberExpression)
            {
                return ParseMemberConstantExpression(memberExpression);
            }
            else if (expression is MethodCallExpression methodCallExpression)
            {
                return ParseMethodCallConstantExpression(methodCallExpression);
            }
            else if (expression is ConditionalExpression conditionalExpression)
            {
                return ParseConditionalExpression(conditionalExpression);
            }
            else if (expression is BinaryExpression binaryExpression)
            {
                var expressionName = expression.GetType().Name;
                if (expressionName == "MethodBinaryExpression")
                {
                    return ParseMethodBinaryExpression(binaryExpression);
                }
                else if (expressionName == "SimpleBinaryExpression")
                {
                    return ParseSimpleBinaryExpression(binaryExpression);
                }
            }
            else if (expression is UnaryExpression unaryExpression)
            {
                if (expression.NodeType == ExpressionType.Convert)
                {
                    return ParseConvertExpression(unaryExpression);
                }
            }

            throw new NotSupportedException($"Unsupported expression type: {expression.GetType()}");
        }

        private static object ParseConstantExpression(ConstantExpression constantExpression)
        {
            return constantExpression.Value;
        }

        private static object ParseMemberConstantExpression(MemberExpression memberExpression)
        {
            if (memberExpression.Expression == null)
            {
                #region 访问静态成员，如：DateTime.Today
                var memberDeclaringType = memberExpression.Member.DeclaringType;
                return GetStaticMemberValue(memberExpression.Member.Name, memberExpression.Member.MemberType, memberDeclaringType);
                #endregion
            }

            #region 访问实例成员
            var value = ParseConstant(memberExpression.Expression);
            return GetInstanceMemberValue(memberExpression.Member.Name, memberExpression.Member.MemberType, value);
            #endregion
        }

        /// <summary>
        /// 获取静态成员的值
        /// </summary>
        /// <param name="memberName"></param>
        /// <param name="memberType"></param>
        /// <param name="memberDeclaringType"></param>
        /// <returns></returns>
        private static object GetStaticMemberValue(string memberName, MemberTypes memberType, Type memberDeclaringType)
        {
            var bindingAttr = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            switch (memberType)
            {
                case MemberTypes.Field:
                    FieldInfo fieldInfo = memberDeclaringType.GetField(memberName, bindingAttr);
                    return fieldInfo.GetValue(memberDeclaringType);
                case MemberTypes.Property:
                    PropertyInfo propertyInfo = memberDeclaringType.GetProperty(memberName, bindingAttr);
                    return propertyInfo.GetValue(memberDeclaringType, null);
                default:
                    throw new NotSupportedException($"Unsupported MemberTypes: {memberType}");
            }
        }

        /// <summary>
        /// 获取实例成员的值
        /// </summary>
        /// <returns></returns>
        private static object GetInstanceMemberValue(string memberName, MemberTypes memberType, object instance)
        {
            var type = instance.GetType();
            var bindingAttr = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            switch (memberType)
            {
                case MemberTypes.Field:
                    FieldInfo fieldInfo = type.GetField(memberName, bindingAttr);
                    return fieldInfo.GetValue(instance);
                case MemberTypes.Property:
                    PropertyInfo propertyInfo = type.GetProperty(memberName, bindingAttr);
                    return propertyInfo.GetValue(instance, null);
                default:
                    throw new NotSupportedException($"Unsupported MemberTypes: {memberType}");
            }
        }

        private static object ParseMethodCallConstantExpression(MethodCallExpression methodCallExpression)
        {
            MethodInfo mi = methodCallExpression.Method;
            object instance = null;
            object[] parameters = null;
            if (methodCallExpression.Object != null)
            {
                instance = ParseConstant(methodCallExpression.Object);
            }
            if (methodCallExpression.Arguments != null && methodCallExpression.Arguments.Count > 0)
            {
                parameters = new object[methodCallExpression.Arguments.Count];
                for (int i = 0; i < methodCallExpression.Arguments.Count; i++)
                {
                    Expression expression = methodCallExpression.Arguments[i];
                    parameters[i] = ParseConstant(expression);
                }
            }

            return mi.Invoke(instance, parameters);
        }

        private static object ParseConditionalExpression(ConditionalExpression conditionalExpression)
        {
            bool condition = (bool)ParseConstant(conditionalExpression.Test);
            if (condition)
            {
                return ParseConstant(conditionalExpression.IfTrue);
            }

            return ParseConstant(conditionalExpression.IfFalse);
        }

        private static object ParseMethodBinaryExpression(BinaryExpression methodBinaryExpression)
        {
            object left = ParseConstant(methodBinaryExpression.Left);
            object right = ParseConstant(methodBinaryExpression.Right);
            MethodInfo methodInfo = methodBinaryExpression.Method;
            if (methodInfo.IsStatic)
            {
                return methodInfo.Invoke(null, new[] { left, right });
            }

            return methodInfo.Invoke(left, new[] { right });
        }

        private static object ParseSimpleBinaryExpression(BinaryExpression simpleBinaryExpression)
        {
            if (simpleBinaryExpression.NodeType == ExpressionType.ArrayIndex)
            {
                var array = ParseConstant(simpleBinaryExpression.Left) as Array;
                var index = (int)ParseConstant(simpleBinaryExpression.Right);
                return array?.GetValue(index);
            }

            return new NotSupportedException();
        }

        private static object ParseConvertExpression(UnaryExpression convertExpression)
        {
            var value = ParseConstant(convertExpression.Operand);
            return ObjectConvert.ChangeType(value, convertExpression.Type);
        }
    }
}
