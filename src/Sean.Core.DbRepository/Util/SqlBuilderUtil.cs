﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Sean.Core.DbRepository.Extensions;

namespace Sean.Core.DbRepository.Util;

internal static class SqlBuilderUtil
{
    #region [Field]
    public static void IncludeFields<TEntity>(List<TableFieldInfoForSqlBuilder> tableFieldList, IEnumerable<string> ignoreFieldNames = null)
    {
        var entityInfo = typeof(TEntity).GetEntityInfo();
        foreach (var field in entityInfo.FieldInfos)
        {
            if (ignoreFieldNames != null && ignoreFieldNames.Any(c => c == field.FieldName))
            {
                continue;
            }

            if (!tableFieldList.Exists(c => c.TableName == entityInfo.TableName && c.FieldName == field.FieldName))
            {
                tableFieldList.Add(new TableFieldInfoForSqlBuilder
                {
                    TableName = entityInfo.TableName,
                    FieldName = field.FieldName,
                    IsPrimaryKey = field.IsPrimaryKey,
                    IsIdentityField = field.IsIdentityField
                });
            }
        }
    }
    public static void IncludeFields(string tableName, List<TableFieldInfoForSqlBuilder> tableFieldList,
        params string[] fields)
    {
        if (fields == null || !fields.Any()) return;

        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field)) continue;

            if (!tableFieldList.Exists(c => c.TableName == tableName && c.FieldName == field))
            {
                tableFieldList.Add(new TableFieldInfoForSqlBuilder
                {
                    TableName = tableName,
                    FieldName = field
                });
            }
        }
    }
    public static void IgnoreFields<TEntity>(string tableName, List<TableFieldInfoForSqlBuilder> tableFieldList,
        params string[] fields)
    {
        if (fields == null || !fields.Any()) return;

        if (!tableFieldList.Any())
        {
            IncludeFields<TEntity>(tableFieldList, fields);
            return;
        }

        tableFieldList.RemoveAll(c => c.TableName == tableName && fields.Contains(c.FieldName));
    }
    public static void PrimaryKeyFields(string tableName, List<TableFieldInfoForSqlBuilder> tableFieldList,
        params string[] fields)
    {
        if (fields == null || !fields.Any()) return;

        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field)) continue;

            var fieldInfo = tableFieldList.Find(c => c.TableName == tableName && c.FieldName == field);
            if (fieldInfo != null)
            {
                fieldInfo.IsPrimaryKey = true;
            }
            else
            {
                tableFieldList.Add(new TableFieldInfoForSqlBuilder
                {
                    TableName = tableName,
                    FieldName = field,
                    IsPrimaryKey = true
                });
            }
        }
    }
    public static void IdentityFields(string tableName, List<TableFieldInfoForSqlBuilder> tableFieldList,
        params string[] fields)
    {
        if (fields == null || !fields.Any()) return;

        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field)) continue;

            var fieldInfo = tableFieldList.Find(c => c.TableName == tableName && c.FieldName == field);
            if (fieldInfo != null)
            {
                fieldInfo.IsIdentityField = true;
            }
            else
            {
                tableFieldList.Add(new TableFieldInfoForSqlBuilder
                {
                    TableName = tableName,
                    FieldName = field,
                    IsIdentityField = true
                });
            }
        }
    }
    public static void IncrementFields<TEntity, TValue>(string tableName, List<TableFieldInfoForSqlBuilder> includeFieldsList,
        Expression<Func<TEntity, object>> fieldExpression, TValue value) where TValue : struct
    {
        var fields = fieldExpression.GetFieldNames();
        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field)) continue;

            var fieldInfo = includeFieldsList.Find(c => c.TableName == tableName && c.FieldName == field);
            if (fieldInfo != null)
            {
                fieldInfo.SetFieldCustomHandler = (fieldName, adapter) => $"{adapter.FormatFieldName(fieldName)} = {adapter.FormatFieldName(fieldName)} + {value}";
            }
            else
            {
                includeFieldsList.Add(new TableFieldInfoForSqlBuilder
                {
                    TableName = tableName,
                    FieldName = field,
                    SetFieldCustomHandler = (fieldName, adapter) => $"{adapter.FormatFieldName(fieldName)} = {adapter.FormatFieldName(fieldName)} + {value}"
                });
            }
        }
    }
    public static void DecrementFields<TEntity, TValue>(string tableName, List<TableFieldInfoForSqlBuilder> includeFieldsList,
        Expression<Func<TEntity, object>> fieldExpression, TValue value) where TValue : struct
    {
        var fields = fieldExpression.GetFieldNames();
        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field)) continue;

            var fieldInfo = includeFieldsList.Find(c => c.TableName == tableName && c.FieldName == field);
            if (fieldInfo != null)
            {
                fieldInfo.SetFieldCustomHandler = (fieldName, adapter) => $"{adapter.FormatFieldName(fieldName)} = {adapter.FormatFieldName(fieldName)} - {value}";
            }
            else
            {
                includeFieldsList.Add(new TableFieldInfoForSqlBuilder
                {
                    TableName = tableName,
                    FieldName = field,
                    SetFieldCustomHandler = (fieldName, adapter) => $"{adapter.FormatFieldName(fieldName)} = {adapter.FormatFieldName(fieldName)} - {value}"
                });
            }
        }
    }
    public static void MaxField(ISqlAdapter sqlAdapter, List<TableFieldInfoForSqlBuilder> tableFieldList,
        string fieldName, string aliasName = null, bool fieldNameFormatted = false)
    {
        if (string.IsNullOrWhiteSpace(fieldName)) return;

        tableFieldList.Add(new TableFieldInfoForSqlBuilder
        {
            TableName = sqlAdapter.TableName,
            FieldName = $"MAX({(!fieldNameFormatted ? sqlAdapter.FormatFieldName(fieldName) : fieldName)})",
            AliasName = aliasName,
            IsFieldNameFormatted = true
        });
    }
    public static void MinField(ISqlAdapter sqlAdapter, List<TableFieldInfoForSqlBuilder> tableFieldList,
        string fieldName, string aliasName = null, bool fieldNameFormatted = false)
    {
        if (string.IsNullOrWhiteSpace(fieldName)) return;

        tableFieldList.Add(new TableFieldInfoForSqlBuilder
        {
            TableName = sqlAdapter.TableName,
            FieldName = $"MIN({(!fieldNameFormatted ? sqlAdapter.FormatFieldName(fieldName) : fieldName)})",
            AliasName = aliasName,
            IsFieldNameFormatted = true
        });
    }
    public static void SumField(ISqlAdapter sqlAdapter, List<TableFieldInfoForSqlBuilder> tableFieldList,
        string fieldName, string aliasName = null, bool fieldNameFormatted = false)
    {
        if (string.IsNullOrWhiteSpace(fieldName)) return;

        tableFieldList.Add(new TableFieldInfoForSqlBuilder
        {
            TableName = sqlAdapter.TableName,
            FieldName = $"SUM({(!fieldNameFormatted ? sqlAdapter.FormatFieldName(fieldName) : fieldName)})",
            AliasName = aliasName,
            IsFieldNameFormatted = true
        });
    }
    public static void AvgField(ISqlAdapter sqlAdapter, List<TableFieldInfoForSqlBuilder> tableFieldList,
        string fieldName, string aliasName = null, bool fieldNameFormatted = false)
    {
        if (string.IsNullOrWhiteSpace(fieldName)) return;

        tableFieldList.Add(new TableFieldInfoForSqlBuilder
        {
            TableName = sqlAdapter.TableName,
            FieldName = $"AVG({(!fieldNameFormatted ? sqlAdapter.FormatFieldName(fieldName) : fieldName)})",
            AliasName = aliasName,
            IsFieldNameFormatted = true
        });
    }
    public static void CountField(ISqlAdapter sqlAdapter, List<TableFieldInfoForSqlBuilder> tableFieldList,
        string fieldName, string aliasName = null, bool fieldNameFormatted = false)
    {
        if (string.IsNullOrWhiteSpace(fieldName)) return;

        tableFieldList.Add(new TableFieldInfoForSqlBuilder
        {
            TableName = sqlAdapter.TableName,
            FieldName = $"COUNT({(!fieldNameFormatted ? sqlAdapter.FormatFieldName(fieldName) : fieldName)})",
            AliasName = aliasName,
            IsFieldNameFormatted = true
        });
    }
    public static void CountDistinctField(ISqlAdapter sqlAdapter, List<TableFieldInfoForSqlBuilder> tableFieldList,
        string fieldName, string aliasName = null)
    {
        if (string.IsNullOrWhiteSpace(fieldName)) return;

        tableFieldList.Add(new TableFieldInfoForSqlBuilder
        {
            TableName = sqlAdapter.TableName,
            FieldName = $"COUNT(DISTINCT {sqlAdapter.FormatFieldName(fieldName)})",
            AliasName = aliasName,
            IsFieldNameFormatted = true
        });
    }
    public static void DistinctFields<TEntity>(ISqlAdapter sqlAdapter, List<TableFieldInfoForSqlBuilder> tableFieldList,
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
        }).ToList());

        tableFieldList.Add(new TableFieldInfoForSqlBuilder
        {
            TableName = sqlAdapter.TableName,
            FieldName = $"DISTINCT {distinctFieldNames}",
            IsFieldNameFormatted = true
        });
    }
    #endregion

    #region [Join Table]
    public static string GetJoinSql<TEntity, TEntity2>(ISqlAdapter sqlAdapter, Expression<Func<TEntity, object>> leftTableFieldExpression, Expression<Func<TEntity2, object>> rightTableFieldExpression, string leftTableAliasName = null, string rightTableAliasName = null)
    {
        var leftTableFields = leftTableFieldExpression.GetFieldNames();
        if (!leftTableFields.Any())
        {
            throw new InvalidOperationException("The specified number of fields must be greater than 0.");
        }
        var rightTableFields = rightTableFieldExpression.GetFieldNames();
        if (leftTableFields.Count != rightTableFields.Count)
        {
            throw new InvalidOperationException("The specified number of fields must be equal.");
        }

        var leftTableName = typeof(TEntity).GetEntityInfo().TableName;
        var rightTableName = typeof(TEntity2).GetEntityInfo().TableName;
        var list = leftTableFields.Select((field, i) => $"{sqlAdapter.FormatFieldName(field, leftTableName, leftTableAliasName)}={sqlAdapter.FormatFieldName(rightTableFields[i], rightTableName, rightTableAliasName)}").ToList();
        return $"{sqlAdapter.FormatTableName(rightTableName)}{(!string.IsNullOrWhiteSpace(rightTableAliasName) ? $" {rightTableAliasName}" : string.Empty)} ON {string.Join(" AND ", list)}";
    }
    #endregion

    #region [WHERE]
    public static void Where(StringBuilder sbWhereClause, string where)
    {
        if (sbWhereClause == null || string.IsNullOrWhiteSpace(where))
        {
            return;
        }

        sbWhereClause.Append(sbWhereClause.Length > 0 ? $" {WhereSqlKeyword.And.ToSqlString()} {where}" : where);
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

        var fields = fieldExpression.GetFieldNames();
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
        sbWhereClause.Append($"{sqlAdapter.FormatFieldName(fieldName)} {operation.ToSqlString()} {sqlAdapter.FormatSqlParameter(parameterName)}");

        if (include == Include.Right)
        {
            sbWhereClause.Append(include.ToSqlString());
        }
    }
    #endregion

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dbType">Database type.</param>
    /// <param name="value"></param>
    /// <param name="convertible">Indicates whether <paramref name="value"/> is convertible.</param>
    /// <returns></returns>
    public static string ConvertToSqlString(DatabaseType dbType, object value, out bool convertible)
    {
        convertible = true;
        if (value == null)
        {
            return "null";
        }

        var type = value.GetType();
        var valueType = Nullable.GetUnderlyingType(type) ?? type;
        if (valueType == typeof(string))
        {
            return $"'{value.ToString().Replace("'", "\\'")}'";
        }

        if (valueType == typeof(bool))
        {
            return dbType switch
            {
                DatabaseType.QuestDB => (bool)value ? "TRUE" : "FALSE",
                DatabaseType.Informix => (bool)value ? "'t'" : "'f'",
                DatabaseType.Xugu => (bool)value ? "true" : "false",
                _ => (bool)value ? "1" : "0"
            };
        }

        if (valueType == typeof(byte)
            || valueType == typeof(sbyte)
            || valueType == typeof(short)
            || valueType == typeof(ushort)
            || valueType == typeof(int)
            || valueType == typeof(uint)
            || valueType == typeof(long)
            || valueType == typeof(ulong)
            || valueType == typeof(float)
            || valueType == typeof(double)
            || valueType == typeof(decimal)
           )
        {
            return $"{value}";
        }

        if (valueType == typeof(DateTime))
        {
            return $"'{value:yyyy-MM-dd HH:mm:ss}'";
        }

        if (valueType.IsEnum)
        {
            return $"{(int)value}";
        }

        if (value is IEnumerable enumerable)// 数组或集合
        {
            var valueStrings = enumerable.Cast<object>().Select(v => ConvertToSqlString(dbType, v, out _));
            return $"({string.Join(",", valueStrings)})";
        }

        return $"'{value.ToString().Replace("'", "\\'")}'";
    }
}