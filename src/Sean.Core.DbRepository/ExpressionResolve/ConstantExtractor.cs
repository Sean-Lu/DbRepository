using System;
using System.Linq.Expressions;
using System.Reflection;
using Sean.Utility.Format;

namespace Sean.Core.DbRepository;

internal static class ConstantExtractor
{
    public static object ParseConstant(Expression expression)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));

        if (expression is ConstantExpression constantExpression)
        {
            return constantExpression.Value;
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
            bool condition = (bool)ParseConstant(conditionalExpression.Test);
            return ParseConstant(condition ? conditionalExpression.IfTrue : conditionalExpression.IfFalse);
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
                if (binaryExpression.NodeType == ExpressionType.ArrayIndex)
                {
                    var array = ParseConstant(binaryExpression.Left) as Array;
                    var index = (int)ParseConstant(binaryExpression.Right);
                    return array?.GetValue(index);
                }

                return new NotSupportedException();
            }
        }
        else if (expression is UnaryExpression unaryExpression)
        {
            if (expression.NodeType == ExpressionType.Convert)
            {
                var value = ParseConstant(unaryExpression.Operand);
                return ObjectConvert.ChangeType(value, unaryExpression.Type);
            }
            else if (expression.NodeType == ExpressionType.Not && expression.Type == typeof(bool))
            {
                return !(bool)ParseConstant(unaryExpression.Operand);
            }
        }

        throw new NotSupportedException($"Unsupported Expression: {expression}");
    }

    private static object ParseMemberConstantExpression(MemberExpression memberExpression)
    {
        if (memberExpression.Expression == null)
        {
            #region 访问静态成员，如：DateTime.Today
            return GetStaticMemberValue(memberExpression);
            #endregion
        }

        #region 访问实例成员
        var value = ParseConstant(memberExpression.Expression);
        return value != null ? GetInstanceMemberValue(memberExpression, value) : null;
        #endregion
    }

    /// <summary>
    /// 获取静态成员的值
    /// </summary>
    /// <param name="memberExpression"></param>
    /// <returns></returns>
    private static object GetStaticMemberValue(MemberExpression memberExpression)
    {
        string memberName = memberExpression.Member.Name;
        MemberTypes memberType = memberExpression.Member.MemberType;
        Type type = memberExpression.Member.DeclaringType;
        var bindingAttr = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        switch (memberType)
        {
            case MemberTypes.Field:
                FieldInfo fieldInfo = type.GetField(memberName, bindingAttr);
                return fieldInfo.GetValue(type);
            case MemberTypes.Property:
                PropertyInfo propertyInfo = type.GetProperty(memberName, bindingAttr);
                return propertyInfo.GetValue(type, null);
            default:
                throw new NotSupportedException($"Unsupported MemberTypes: {memberType}");
        }
    }

    /// <summary>
    /// 获取实例成员的值
    /// </summary>
    /// <param name="memberExpression"></param>
    /// <param name="instance"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    private static object GetInstanceMemberValue(MemberExpression memberExpression, object instance)
    {
        string memberName = memberExpression.Member.Name;
        MemberTypes memberType = memberExpression.Member.MemberType;
        Type type = memberExpression.Member.DeclaringType;//instance.GetType();
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

    private static object ParseMethodBinaryExpression(BinaryExpression methodBinaryExpression)
    {
        object left = ParseConstant(methodBinaryExpression.Left);
        object right = ParseConstant(methodBinaryExpression.Right);
        MethodInfo methodInfo = methodBinaryExpression.Method;
        if (methodInfo != null && methodInfo.IsStatic)
        {
            return methodInfo.Invoke(null, new[] { left, right });
        }

        return methodInfo?.Invoke(left, new[] { right });
    }
}