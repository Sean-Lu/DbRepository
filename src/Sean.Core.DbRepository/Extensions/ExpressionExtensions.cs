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
                        throw new InvalidOperationException($"[Expression]表达式不支持的SQL入参类型：{sqlFactory.Parameter.GetType().FullName}");
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
                    if (!result.Contains(memberName))
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
                        if (!result.Contains(memberName))
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
            else
            {
                // 单个字段
                var memberName = fieldExpression.GetMemberName();
                if (!result.Contains(memberName))
                {
                    result.Add(memberName);
                }
            }

            if (!result.Any())
            {
                throw new ArgumentException($"Unsupported expression type: {fieldExpression.NodeType}", nameof(fieldExpression));
            }

            return result;
        }
        private static string GetMemberName(this Expression expression)
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
                throw new ArgumentException($"Unsupported expression type: {expression.NodeType}", nameof(expression));
            }

            return memberName;
        }
        #endregion
    }
}
