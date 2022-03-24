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
        /// 解析WHERE过滤条件
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="whereExpression"></param>
        /// <param name="sqlFactory"></param>
        /// <returns></returns>
        public static SqlFactory<TEntity> ResolveWhereExpression<TEntity>(this Expression<Func<TEntity, bool>> whereExpression, SqlFactory<TEntity> sqlFactory)
        {
            Dictionary<string, object> parameters;
            if (sqlFactory.Parameter != null)
            {
                if (sqlFactory.Parameter is Dictionary<string, object> oldParameter)
                {
                    parameters = oldParameter;
                }
                else
                {
                    var paramDic = sqlFactory.ConvertToParameter(sqlFactory.Parameter);
                    if (paramDic != null)
                    {
                        parameters = paramDic;
                    }
                    else
                    {
                        throw new InvalidOperationException($"SQL input parameter type not supported by expression: {sqlFactory.Parameter.GetType().FullName}");
                    }
                }
            }
            else
            {
                parameters = new Dictionary<string, object>();
            }

            var whereClause = whereExpression.GetParameterizedWhereClause(sqlFactory.SqlAdapter, parameters);
            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                sqlFactory.Where(whereClause);
            }
            if (parameters.Any())
            {
                sqlFactory.SetParameter(parameters);
            }
            return sqlFactory;
        }
        /// <summary>
        /// 获取参数化WHERE子句
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="whereExpression"></param>
        /// <param name="sqlAdapter"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static string GetParameterizedWhereClause<TEntity>(this Expression<Func<TEntity, bool>> whereExpression, ISqlAdapter sqlAdapter, Dictionary<string, object> parameters)
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
        public static string GetParameterizedWhereClause<TEntity>(this Expression<Func<TEntity, bool>> whereExpression, ISqlAdapter sqlAdapter, out Dictionary<string, object> parameters)
        {
            parameters = new Dictionary<string, object>();
            return whereExpression.GetParameterizedWhereClause(sqlAdapter, parameters);
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
                foreach (var memberInfo in newExpression.Members)
                {
                    var memberName = memberInfo.Name;
                    if (!result.Contains(memberName))
                    {
                        result.Add(memberName);
                    }
                }
            }
            else if (fieldExpression is MemberExpression memberExpression
                     && memberExpression.Expression is ConstantExpression constantExpression)
            {
                var value = constantExpression.Value;
                if (value != null)
                {
                    var valueType = value.GetType();
                    var fieldInfo = valueType.GetField(memberExpression.Member.Name);
                    if (fieldInfo != null)
                    {
                        var actualValue = fieldInfo.GetValue(value);
                        if (actualValue is IEnumerable<string> fields)
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
                // 单个字段
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

            if (expression is UnaryExpression unaryExpression)
            {
                if (unaryExpression.Operand is MemberExpression memberExpression)
                {
                    return memberExpression.Member.Name;
                }
            }
            else if (expression is ConstantExpression constantExpression)
            {
                return constantExpression.Value as string;
            }
            else if (expression is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }

            throw new NotSupportedException($"Unsupported expression type: {expression.GetType()}");
        }
        #endregion
    }
}
