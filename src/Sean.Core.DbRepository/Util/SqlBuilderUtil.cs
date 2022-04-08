using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository
{
    public class SqlBuilderUtil
    {
        #region [Join] 表关联
        public static string GetJoinFields<TEntity, TEntity2>(ISqlAdapter sqlAdapter, Expression<Func<TEntity, object>> fieldExpression, Expression<Func<TEntity2, object>> fieldExpression2, string joinTableName)
        {
            var fields = fieldExpression.GetMemberNames();
            var fields2 = fieldExpression2.GetMemberNames();
            if (!fields.Any())
            {
                throw new InvalidOperationException("The specified number of fields must be greater than 0.");
            }
            if (fields.Count != fields2.Count)
            {
                throw new InvalidOperationException("The specified number of fields must be equal.");
            }

            var list = new List<string>();
            for (var i = 0; i < fields.Count; i++)
            {
                list.Add($"{sqlAdapter.FormatFieldName(fields[i], true)}={sqlAdapter.FormatFieldName(fields2[i], joinTableName)}");
            }

            return string.Join(" AND ", list);
        }
        #endregion

        #region [WHERE]
        public static void Where(StringBuilder sbWhereClause, WhereSqlKeyword keyword,
            string where)
        {
            if (!string.IsNullOrWhiteSpace(where))
            {
                if (sbWhereClause.Length > 0) sbWhereClause.Append(" ");
                else if (keyword == WhereSqlKeyword.And) sbWhereClause.Append("1=1 ");

                sbWhereClause.Append(keyword == WhereSqlKeyword.None ? where : $"{keyword.ToSqlString()} {where}");
            }
        }
        public static void Where<TEntity>(ISqlAdapter sqlAdapter, Dictionary<string, object> dicParameters, Action<string> setWhereClause, Action<Dictionary<string, object>> setParameter,
            Expression<Func<TEntity, bool>> whereExpression)
        {
            if (whereExpression != null)
            {
                var whereClause = whereExpression.GetParameterizedWhereClause(sqlAdapter, dicParameters);
                if (!string.IsNullOrWhiteSpace(whereClause))
                {
                    setWhereClause(whereClause);
                }
                if (dicParameters.Any())
                {
                    setParameter(dicParameters);
                }
            }
        }
        public static void WhereField<TEntity>(ISqlAdapter sqlAdapter, StringBuilder sbWhereClause,
            Expression<Func<TEntity, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null)
        {
            if (fieldExpression != null)
            {
                var fields = fieldExpression.GetMemberNames();
                if (fields == null || fields.Count != 1)
                {
                    throw new InvalidOperationException($"[{nameof(WhereField)}]Field must be specified, and the number can only be one.");
                }

                var fieldName = fields.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(fieldName))
                {
                    if (sbWhereClause.Length > 0) sbWhereClause.Append(" ");
                    else if (keyword == WhereSqlKeyword.And) sbWhereClause.Append("1=1 ");

                    var keywordSqlString = keyword.ToSqlString();
                    if (!string.IsNullOrWhiteSpace(keywordSqlString))
                    {
                        sbWhereClause.Append($"{keywordSqlString} ");
                    }

                    if (include == Include.Left)
                    {
                        sbWhereClause.Append(include.ToSqlString());
                    }
                    sbWhereClause.Append($"{sqlAdapter.FormatFieldName(fieldName)} {operation.ToSqlString()} {sqlAdapter.FormatInputParameter(paramName ?? fieldName)}");
                    if (include == Include.Right)
                    {
                        sbWhereClause.Append(include.ToSqlString());
                    }
                }
            }
        }
        #endregion
    }
}
