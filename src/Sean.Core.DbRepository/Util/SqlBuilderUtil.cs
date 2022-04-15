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
        #region [Field]
        public static void IncludeFields(ISqlAdapter sqlAdapter, List<TableFieldInfoForSqlBuilder> includeFieldsList,
            params string[] fields)
        {
            if (fields == null || !fields.Any()) return;

            foreach (var field in fields)
            {
                if (string.IsNullOrWhiteSpace(field)) continue;

                if (!includeFieldsList.Exists(c => c.TableName == sqlAdapter.TableName && c.FieldName == field))
                {
                    includeFieldsList.Add(new TableFieldInfoForSqlBuilder
                    {
                        TableName = sqlAdapter.TableName,
                        FieldName = field
                    });
                }
            }
        }
        public static void IgnoreFields<TEntity>(ISqlAdapter sqlAdapter, List<TableFieldInfoForSqlBuilder> includeFieldsList,
            params string[] fields)
        {
            if (fields == null || !fields.Any()) return;

            if (!includeFieldsList.Any())
            {
                IncludeFields(sqlAdapter, includeFieldsList, typeof(TEntity).GetAllFieldNames().Except(fields).ToArray());
                return;
            }

            includeFieldsList.RemoveAll(c => c.TableName == sqlAdapter.TableName && fields.Contains(c.FieldName));
        }
        public static void PrimaryKeyFields(ISqlAdapter sqlAdapter, List<TableFieldInfoForSqlBuilder> includeFieldsList,
            params string[] fields)
        {
            if (fields == null || !fields.Any()) return;

            foreach (var field in fields)
            {
                if (string.IsNullOrWhiteSpace(field)) continue;

                var fieldInfo = includeFieldsList.Find(c => c.TableName == sqlAdapter.TableName && c.FieldName == field);
                if (fieldInfo != null)
                {
                    fieldInfo.PrimaryKey = true;
                }
                else
                {
                    includeFieldsList.Add(new TableFieldInfoForSqlBuilder
                    {
                        TableName = sqlAdapter.TableName,
                        FieldName = field,
                        PrimaryKey = true
                    });
                }
            }
        }
        public static void IdentityFields(ISqlAdapter sqlAdapter, List<TableFieldInfoForSqlBuilder> includeFieldsList,
            params string[] fields)
        {
            if (fields == null || !fields.Any()) return;

            foreach (var field in fields)
            {
                if (string.IsNullOrWhiteSpace(field)) continue;

                var fieldInfo = includeFieldsList.Find(c => c.TableName == sqlAdapter.TableName && c.FieldName == field);
                if (fieldInfo != null)
                {
                    fieldInfo.Identity = true;
                }
                else
                {
                    includeFieldsList.Add(new TableFieldInfoForSqlBuilder
                    {
                        TableName = sqlAdapter.TableName,
                        FieldName = field,
                        Identity = true
                    });
                }
            }
        }
        public static void IncrFields<TEntity, TValue>(ISqlAdapter sqlAdapter, List<TableFieldInfoForSqlBuilder> includeFieldsList,
            Expression<Func<TEntity, object>> fieldExpression, TValue value) where TValue : struct
        {
            var fields = fieldExpression.GetMemberNames();
            foreach (var field in fields)
            {
                if (string.IsNullOrWhiteSpace(field)) continue;

                var fieldInfo = includeFieldsList.Find(c => c.TableName == sqlAdapter.TableName && c.FieldName == field);
                if (fieldInfo != null)
                {
                    fieldInfo.SetFieldCustomHandler = (fieldName, adapter) => $"{adapter.FormatFieldName(fieldName)} = {adapter.FormatFieldName(fieldName)} + {value}";
                }
                else
                {
                    includeFieldsList.Add(new TableFieldInfoForSqlBuilder
                    {
                        TableName = sqlAdapter.TableName,
                        FieldName = field,
                        SetFieldCustomHandler = (fieldName, adapter) => $"{adapter.FormatFieldName(fieldName)} = {adapter.FormatFieldName(fieldName)} + {value}"
                    });
                }
            }
        }
        public static void DecrFields<TEntity, TValue>(ISqlAdapter sqlAdapter, List<TableFieldInfoForSqlBuilder> includeFieldsList,
            Expression<Func<TEntity, object>> fieldExpression, TValue value) where TValue : struct
        {
            var fields = fieldExpression.GetMemberNames();
            foreach (var field in fields)
            {
                if (string.IsNullOrWhiteSpace(field)) continue;

                var fieldInfo = includeFieldsList.Find(c => c.TableName == sqlAdapter.TableName && c.FieldName == field);
                if (fieldInfo != null)
                {
                    fieldInfo.SetFieldCustomHandler = (fieldName, adapter) => $"{adapter.FormatFieldName(fieldName)} = {adapter.FormatFieldName(fieldName)} - {value}";
                }
                else
                {
                    includeFieldsList.Add(new TableFieldInfoForSqlBuilder
                    {
                        TableName = sqlAdapter.TableName,
                        FieldName = field,
                        SetFieldCustomHandler = (fieldName, adapter) => $"{adapter.FormatFieldName(fieldName)} = {adapter.FormatFieldName(fieldName)} - {value}"
                    });
                }
            }
        }
        public static void MaxField(ISqlAdapter sqlAdapter, List<TableFieldInfoForSqlBuilder> includeFieldsList,
            string fieldName, string aliasName = null)
        {
            if (string.IsNullOrWhiteSpace(fieldName)) return;

            includeFieldsList.Add(new TableFieldInfoForSqlBuilder
            {
                TableName = sqlAdapter.TableName,
                FieldName = $"MAX({sqlAdapter.FormatFieldName(fieldName)})",
                AliasName = aliasName,
                FieldNameFormatted = true
            });
        }
        public static void MinField(ISqlAdapter sqlAdapter, List<TableFieldInfoForSqlBuilder> includeFieldsList,
            string fieldName, string aliasName = null)
        {
            if (string.IsNullOrWhiteSpace(fieldName)) return;

            includeFieldsList.Add(new TableFieldInfoForSqlBuilder
            {
                TableName = sqlAdapter.TableName,
                FieldName = $"MIN({sqlAdapter.FormatFieldName(fieldName)})",
                AliasName = aliasName,
                FieldNameFormatted = true
            });
        }
        public static void SumField(ISqlAdapter sqlAdapter, List<TableFieldInfoForSqlBuilder> includeFieldsList,
            string fieldName, string aliasName = null)
        {
            if (string.IsNullOrWhiteSpace(fieldName)) return;

            includeFieldsList.Add(new TableFieldInfoForSqlBuilder
            {
                TableName = sqlAdapter.TableName,
                FieldName = $"SUM({sqlAdapter.FormatFieldName(fieldName)})",
                AliasName = aliasName,
                FieldNameFormatted = true
            });
        }
        public static void AvgField(ISqlAdapter sqlAdapter, List<TableFieldInfoForSqlBuilder> includeFieldsList,
            string fieldName, string aliasName = null)
        {
            if (string.IsNullOrWhiteSpace(fieldName)) return;

            includeFieldsList.Add(new TableFieldInfoForSqlBuilder
            {
                TableName = sqlAdapter.TableName,
                FieldName = $"AVG({sqlAdapter.FormatFieldName(fieldName)})",
                AliasName = aliasName,
                FieldNameFormatted = true
            });
        }
        public static void CountField(ISqlAdapter sqlAdapter, List<TableFieldInfoForSqlBuilder> includeFieldsList,
            string fieldName, string aliasName = null)
        {
            if (string.IsNullOrWhiteSpace(fieldName)) return;

            includeFieldsList.Add(new TableFieldInfoForSqlBuilder
            {
                TableName = sqlAdapter.TableName,
                FieldName = $"COUNT({sqlAdapter.FormatFieldName(fieldName)})",
                AliasName = aliasName,
                FieldNameFormatted = true
            });
        }
        public static void CountDistinctField(ISqlAdapter sqlAdapter, List<TableFieldInfoForSqlBuilder> includeFieldsList,
            string fieldName, string aliasName = null)
        {
            if (string.IsNullOrWhiteSpace(fieldName)) return;

            includeFieldsList.Add(new TableFieldInfoForSqlBuilder
            {
                TableName = sqlAdapter.TableName,
                FieldName = $"COUNT(DISTINCT {sqlAdapter.FormatFieldName(fieldName)})",
                AliasName = aliasName,
                FieldNameFormatted = true
            });
        }
        public static void DistinctFields<TEntity>(ISqlAdapter sqlAdapter, List<TableFieldInfoForSqlBuilder> includeFieldsList,
            params string[] fields)
        {
            if (fields == null || !fields.Any()) return;

            var tableFieldInfos = typeof(TEntity).GetEntityInfo().FieldInfos;
            var distinctFieldNames = string.Join(",", fields.Select(fieldName =>
            {
                var findFieldInfo = tableFieldInfos.Find(c => c.FieldName == fieldName);
                if (findFieldInfo != null && findFieldInfo.Property.Name != fieldName)
                {
                    return $"{sqlAdapter.FormatFieldName(fieldName)} AS {findFieldInfo.Property.Name}"; // SELECT column_name AS alias_name
                }

                return $"{sqlAdapter.FormatFieldName(fieldName)}";
            }));

            includeFieldsList.Add(new TableFieldInfoForSqlBuilder
            {
                TableName = sqlAdapter.TableName,
                FieldName = $"DISTINCT {sqlAdapter.FormatFieldName(distinctFieldNames)}",
                FieldNameFormatted = true
            });
        }
        #endregion

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
            if (string.IsNullOrWhiteSpace(where)) return;

            if (sbWhereClause.Length > 0) sbWhereClause.Append(" ");
            else if (keyword == WhereSqlKeyword.And) sbWhereClause.Append("1=1 ");

            sbWhereClause.Append(keyword == WhereSqlKeyword.None ? where : $"{keyword.ToSqlString()} {where}");
        }
        public static void Where<TEntity>(ISqlAdapter sqlAdapter, IDictionary<string, object> dicParameters, Action<string> setWhereClause, Action<IDictionary<string, object>> setParameter,
            Expression<Func<TEntity, bool>> whereExpression)
        {
            if (whereExpression == null) return;

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
        public static void WhereField<TEntity>(ISqlAdapter sqlAdapter, StringBuilder sbWhereClause,
            Expression<Func<TEntity, object>> fieldExpression, SqlOperation operation, WhereSqlKeyword keyword = WhereSqlKeyword.And, Include include = Include.None, string paramName = null)
        {
            if (fieldExpression == null) return;

            var fields = fieldExpression.GetMemberNames();
            if (fields == null || fields.Count != 1)
            {
                throw new InvalidOperationException($"[{nameof(WhereField)}-{nameof(fieldExpression)}]The fields in WHERE clause must be specified, and the number of fields can only be one.");
            }

            var fieldName = fields.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(fieldName)) return;

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

            var parameterName = paramName;
            if (parameterName == null)
            {
                var tableFieldInfos = typeof(TEntity).GetEntityInfo().FieldInfos;
                var findFieldInfo = tableFieldInfos.Find(c => c.FieldName == fieldName);
                parameterName = findFieldInfo?.Property.Name ?? fieldName;
            }
            sbWhereClause.Append($"{sqlAdapter.FormatFieldName(fieldName)} {operation.ToSqlString()} {sqlAdapter.FormatInputParameter(parameterName)}");

            if (include == Include.Right)
            {
                sbWhereClause.Append(include.ToSqlString());
            }
        }
        #endregion
    }
}
