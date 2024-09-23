using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Sean.Core.DbRepository.Util;

namespace Sean.Core.DbRepository.Extensions;

public static class ExpressionExtensions
{
    #region fieldExpression
    public static List<string> GetFieldNames<TEntity>(this Expression<Func<TEntity, object>> fieldExpression)
    {
        return fieldExpression?.Body.GetFieldNames(typeof(TEntity).GetEntityInfo().NamingConvention)?.Distinct().ToList();
    }

    public static bool IsFieldExists<TEntity>(this Expression<Func<TEntity, object>> fieldExpression, string fieldName)
    {
        if (fieldExpression == null || string.IsNullOrEmpty(fieldName))
        {
            return false;
        }

        var fields = fieldExpression.GetFieldNames();
        return fields != null && fields.Exists(c => c == fieldName);
    }
    public static bool IsFieldExists<TEntity>(this Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, object>> matchFieldExpression)
    {
        if (fieldExpression == null || matchFieldExpression == null)
        {
            return false;
        }

        var fields = fieldExpression.GetFieldNames();
        var matchFields = matchFieldExpression.GetFieldNames();
        return fields != null && matchFields != null && matchFields.TrueForAll(fieldName => fields.Exists(c => c == fieldName));
    }

    public static Expression<Func<TEntity, object>> AddFieldNames<TEntity>(this Expression<Func<TEntity, object>> fieldExpression, params string[] fieldNames)
    {
        if (fieldExpression == null || fieldNames == null || !fieldNames.Any())
        {
            return fieldExpression;
        }

        var fields = fieldExpression.GetFieldNames();
        fields.AddRange(fieldNames);
        return ExpressionUtil.CreateFieldExpression<TEntity>(fields.Distinct());
    }
    public static Expression<Func<TEntity, object>> AddFieldNames<TEntity>(this Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity, object>> addFieldExpression)
    {
        if (fieldExpression == null || addFieldExpression == null)
        {
            return fieldExpression;
        }

        var fields = fieldExpression.GetFieldNames();
        var addFields = addFieldExpression.GetFieldNames();
        if (addFields == null || !addFields.Any())
        {
            return fieldExpression;
        }
        fields.AddRange(addFields);
        return ExpressionUtil.CreateFieldExpression<TEntity>(fields.Distinct());
    }
    #endregion

    #region whereExpression
    /// <summary>
    /// Gets the parameterized WHERE clause.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
    /// <param name="sqlAdapter"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public static string GetParameterizedWhereClause<TEntity>(this Expression<Func<TEntity, bool>> whereExpression, ISqlAdapter sqlAdapter, IDictionary<string, object> parameters)
    {
        var adhesive = new WhereClauseAdhesive(sqlAdapter, parameters);
        return WhereClauseParser.Parse(whereExpression, adhesive);
    }
    /// <summary>
    /// Gets the parameterized WHERE clause.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="whereExpression">Lambda expression representing an SQL WHERE condition.</param>
    /// <param name="sqlAdapter"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public static string GetParameterizedWhereClause<TEntity>(this Expression<Func<TEntity, bool>> whereExpression, ISqlAdapter sqlAdapter, out IDictionary<string, object> parameters)
    {
        parameters = new Dictionary<string, object>();
        return whereExpression.GetParameterizedWhereClause(sqlAdapter, parameters);
    }

    public static Expression<Func<TEntity, bool>> AndAlsoIF<TEntity>(this Expression<Func<TEntity, bool>> whereExpression, bool condition, Expression<Func<TEntity, bool>> mergeWhereExpression)
    {
        return condition ? whereExpression.AndAlso(mergeWhereExpression) : whereExpression;
    }
    public static Expression<Func<TEntity, bool>> AndAlsoIF<TEntity>(this Expression<Func<TEntity, bool>> whereExpression, bool condition, Expression<Func<TEntity, bool>> trueWhereExpression, Expression<Func<TEntity, bool>> falseWhereExpression)
    {
        return whereExpression.AndAlso(condition ? trueWhereExpression : falseWhereExpression);
    }

    public static Expression<Func<TEntity, bool>> AndAlso<TEntity>(this Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, bool>> mergeWhereExpression)
    {
        return whereExpression.Compose(mergeWhereExpression, Expression.AndAlso);
    }

    public static Expression<Func<TEntity, bool>> OrElse<TEntity>(this Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, bool>> mergeWhereExpression)
    {
        return whereExpression.Compose(mergeWhereExpression, Expression.OrElse);
    }
    #endregion

    #region Private method
    /// <summary>
    /// 动态拼接 Expression 表达式
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <param name="merge"></param>
    /// <returns></returns>
    private static Expression<T> Compose<T>(this Expression<T> first, Expression<T> second, Func<Expression, Expression, Expression> merge)
    {
        // 构建参数映射（从第二个参数到第一个参数）
        var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] }).ToDictionary(p => p.s, p => p.f);
        // 用第一个lambda表达式中的参数替换第二个lambda表达式中的参数
        var secondBody = ExpressionParameterRebinder.ReplaceParameters(map, second.Body);
        // 将lambda表达式体的组合应用于来自第一个表达式的参数
        return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
    }

    private static List<string> GetFieldNames(this Expression fieldExpression, NamingConvention namingConvention)
    {
        if (fieldExpression == null) throw new ArgumentNullException(nameof(fieldExpression));

        var result = new List<string>();
        if (fieldExpression is ConstantExpression constantExpression2)
        {
            var value = constantExpression2.Value;
            if (value is IEnumerable<string> fieldNames)
            {
                foreach (var fieldName in fieldNames)
                {
                    if (!result.Contains(fieldName))
                    {
                        result.Add(fieldName);
                    }
                }
            }
            else if (value is string fieldName)
            {
                if (!result.Contains(fieldName))
                {
                    result.Add(fieldName);
                }
            }
        }
        else if (fieldExpression is NewArrayExpression newArrayExpression)// 数组
        {
            foreach (var subExpression in newArrayExpression.Expressions)
            {
                var memberName = subExpression.GetFieldName(namingConvention);
                if (!string.IsNullOrWhiteSpace(memberName) && !result.Contains(memberName))
                {
                    result.Add(memberName);
                }
            }
        }
        else if (fieldExpression is ListInitExpression listInitExpression)// List泛型集合
        {
            foreach (var initializer in listInitExpression.Initializers)
            {
                foreach (var argument in initializer.Arguments)
                {
                    var memberName = argument.GetFieldName(namingConvention);
                    if (!string.IsNullOrWhiteSpace(memberName) && !result.Contains(memberName))
                    {
                        result.Add(memberName);
                    }
                }
            }
        }
        else if (fieldExpression is NewExpression newExpression)// 匿名类型
        {
            //foreach (var memberInfo in newExpression.Members)
            //{
            //    var memberName = memberInfo.GetFieldName();
            //    if (!result.Contains(memberName))
            //    {
            //        result.Add(memberName);
            //    }
            //}
            foreach (var argument in newExpression.Arguments)
            {
                var memberName = argument.GetFieldName(namingConvention);
                if (!string.IsNullOrWhiteSpace(memberName) && !result.Contains(memberName))
                {
                    result.Add(memberName);
                }
            }
        }
        else if (fieldExpression is MemberExpression memberExpression)
        {
            if (memberExpression.Expression is ParameterExpression parameterExpression)
            {
                var fieldName = memberExpression.Member.GetFieldName(namingConvention);
                if (!string.IsNullOrWhiteSpace(fieldName) && !result.Contains(fieldName))
                {
                    result.Add(fieldName);
                }
            }
            else if (memberExpression.Expression is ConstantExpression constantExpression)
            {
                var value = constantExpression.Value;
                if (value != null)
                {
                    var valueType = value.GetType();
                    var fieldInfo = valueType.GetField(memberExpression.Member.Name);
                    if (fieldInfo != null)
                    {
                        var actualValue = fieldInfo.GetValue(value);
                        if (actualValue is string strValue)
                        {
                            if (!result.Contains(strValue))
                            {
                                result.Add(strValue);
                            }
                        }
                        else if (actualValue is IEnumerable<string> fields)
                        {
                            foreach (var field in fields)
                            {
                                if (!string.IsNullOrWhiteSpace(field) && !result.Contains(field))
                                {
                                    result.Add(field);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                var value = ConstantExtractor.ParseConstant(memberExpression);
                if (value is string strValue)
                {
                    if (!result.Contains(strValue))
                    {
                        result.Add(strValue);
                    }
                }
                else if (value is IEnumerable<string> fields)
                {
                    foreach (var field in fields)
                    {
                        if (!string.IsNullOrWhiteSpace(field) && !result.Contains(field))
                        {
                            result.Add(field);
                        }
                    }
                }
            }
        }
        else if (fieldExpression is MethodCallExpression methodCallExpression)
        {
            var value = ConstantExtractor.ParseConstant(methodCallExpression);
            if (value is IEnumerable<string> fields)
            {
                foreach (var field in fields)
                {
                    if (!string.IsNullOrWhiteSpace(field) && !result.Contains(field))
                    {
                        result.Add(field);
                    }
                }
            }
        }

        if (!result.Any())
        {
            var memberName = fieldExpression.GetFieldName(namingConvention);
            if (!string.IsNullOrWhiteSpace(memberName) && !result.Contains(memberName))
            {
                result.Add(memberName);
            }
        }

        if (!result.Any())
        {
            throw new NotSupportedException($"Unsupported expression type: {fieldExpression.GetType()}");
        }

        return result;
    }
    private static string GetFieldName(this Expression expression, NamingConvention namingConvention)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));

        string result = null;
        if (expression is UnaryExpression unaryExpression)
        {
            if (unaryExpression.Operand is MemberExpression memberExpression)
            {
                result = memberExpression.Member.GetFieldName(namingConvention);
            }
        }
        else if (expression is ConstantExpression constantExpression)
        {
            result = constantExpression.Value as string;
        }
        else if (expression is MemberExpression memberExpression)
        {
            if (memberExpression.Expression is ParameterExpression)
            {
                result = memberExpression.Member.GetFieldName(namingConvention);
            }
            else
            {
                var value = ConstantExtractor.ParseConstant(memberExpression);
                result = value as string;
            }
        }

        return result ?? throw new NotSupportedException($"Unsupported expression type: {expression.GetType()}");
    }
    #endregion
}