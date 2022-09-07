using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Sean.Core.DbRepository.Extensions
{
    public static class ExpressionExtensions
    {
        #region fieldExpression
        /// <summary>
        /// 获取表达式树的属性成员名称
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="fieldExpression">示例：
        /// <para>单个字段：entity => entity.Status</para>
        /// <para>多个字段（匿名类型）：entity => new { entity.Status, entity.UpdateTime }</para>
        /// </param>
        /// <returns></returns>
        public static List<string> GetMemberNames<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> fieldExpression)
        {
            return fieldExpression.Body.GetMemberNames();
        }
        #endregion

        #region whereExpression
        /// <summary>
        /// 获取参数化WHERE子句
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="whereExpression"></param>
        /// <param name="sqlAdapter"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static string GetParameterizedWhereClause<TEntity>(this Expression<Func<TEntity, bool>> whereExpression, ISqlAdapter sqlAdapter, IDictionary<string, object> parameters)
        {
            var adhesive = new WhereClauseAdhesive(sqlAdapter, parameters);
            var whereClause = WhereCaluseParser.Parse(whereExpression.Body, adhesive);
            return whereClause.ToString();
        }
        /// <summary>
        /// 获取参数化WHERE子句
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="whereExpression"></param>
        /// <param name="sqlAdapter"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static string GetParameterizedWhereClause<TEntity>(this Expression<Func<TEntity, bool>> whereExpression, ISqlAdapter sqlAdapter, out IDictionary<string, object> parameters)
        {
            parameters = new Dictionary<string, object>();
            return whereExpression.GetParameterizedWhereClause(sqlAdapter, parameters);
        }
        #endregion

        #region 动态拼接 Expression 表达式
        public static Expression<T> Compose<T>(this Expression<T> first, Expression<T> second, Func<Expression, Expression, Expression> merge)
        {
            // 构建参数映射（从第二个参数到第一个参数）
            var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] }).ToDictionary(p => p.s, p => p.f);
            // 用第一个lambda表达式中的参数替换第二个lambda表达式中的参数
            var secondBody = ExpressionParameterRebinder.ReplaceParameters(map, second.Body);
            // 将lambda表达式体的组合应用于来自第一个表达式的参数
            return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
        }
        public static Expression<Func<T, bool>> AndAlso<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.AndAlso);
        }
        public static Expression<Func<T, bool>> OrElse<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.OrElse);
        }

        #endregion

        #region Private method
        private static List<string> GetMemberNames(this Expression fieldExpression)
        {
            if (fieldExpression == null) throw new ArgumentNullException(nameof(fieldExpression));

            var result = new List<string>();
            if (fieldExpression is NewArrayExpression newArrayExpression)// 数组
            {
                foreach (var subExpression in newArrayExpression.Expressions)
                {
                    var memberName = subExpression.GetMemberName();
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
                        var memberName = argument.GetMemberName();
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
                    var memberName = argument.GetMemberName();
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
                    var fieldName = memberExpression.Member.GetFieldName();
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
            else
            {
                var memberName = fieldExpression.GetMemberName();
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
        private static string GetMemberName(this Expression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            string result = null;
            if (expression is UnaryExpression unaryExpression)
            {
                if (unaryExpression.Operand is MemberExpression memberExpression)
                {
                    result = memberExpression.Member.GetFieldName();
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
                    result = memberExpression.Member.GetFieldName();
                }
                else
                {
                    var value = ConstantExtractor.ParseConstant(memberExpression);
                    result = value as string;
                }
            }

            if (result == null) throw new NotSupportedException($"Unsupported expression type: {expression.GetType()}");
            return result;
        }
        #endregion
    }
}
