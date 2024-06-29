using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Sean.Core.DbRepository.Extensions;
using Sean.Core.DbRepository.Util;

namespace Sean.Core.DbRepository;

public class ReplaceableSqlBuilder<TEntity> : BaseSqlBuilder, IReplaceable<TEntity>
{
    private const string SqlTemplate = "REPLACE INTO {0}({1}) VALUES{2}";
    private const string SqlIndentedTemplate = @"REPLACE INTO {0}({1}) 
VALUES{2}";

    private readonly List<TableFieldInfoForSqlBuilder> _tableFieldList = new();
    private object _parameter;

    private ReplaceableSqlBuilder(DatabaseType dbType, string tableName) : base(dbType, tableName)
    {

    }

    /// <summary>
    /// Create an instance of <see cref="IReplaceable{TEntity}"/>.
    /// </summary>
    /// <param name="dbType">Database type.</param>
    /// <param name="autoIncludeFields">Whether all table fields are automatically resolved from <typeparamref name="TEntity"/>.</param>
    /// <param name="tableName">The table name.</param>
    /// <returns></returns>
    public static IReplaceable<TEntity> Create(DatabaseType dbType, bool autoIncludeFields, string tableName = null)
    {
        var sqlBuilder = new ReplaceableSqlBuilder<TEntity>(dbType, tableName ?? typeof(TEntity).GetEntityInfo().TableName);
        if (autoIncludeFields)
        {
            sqlBuilder.InsertFields(typeof(TEntity).GetEntityInfo().FieldInfos.Select(c => c.FieldName).ToArray());
        }
        return sqlBuilder;
    }

    #region [Field]
    public virtual IReplaceable<TEntity> InsertFields(params string[] fields)
    {
        SqlBuilderUtil.IncludeFields(SqlAdapter, _tableFieldList, fields);
        return this;
    }
    public virtual IReplaceable<TEntity> IgnoreFields(params string[] fields)
    {
        SqlBuilderUtil.IgnoreFields<TEntity>(SqlAdapter, _tableFieldList, fields);
        return this;
    }

    public virtual IReplaceable<TEntity> InsertFields(Expression<Func<TEntity, object>> fieldExpression)
    {
        if (fieldExpression == null) return this;
        var fields = fieldExpression.GetFieldNames().ToArray();
        return InsertFields(fields);
    }
    public virtual IReplaceable<TEntity> IgnoreFields(Expression<Func<TEntity, object>> fieldExpression)
    {
        if (fieldExpression == null) return this;
        var fields = fieldExpression.GetFieldNames().ToArray();
        return IgnoreFields(fields);
    }
    #endregion

    public virtual IReplaceable<TEntity> SetParameter(object param)
    {
        _parameter = param;
        return this;
    }

    protected override ISqlCommand BuildSqlCommand()
    {
        var fields = _tableFieldList;
        if (!fields.Any())
            return default;

        var sb = new StringBuilder();
        var formatFields = fields.Select(fieldInfo => SqlAdapter.FormatFieldName(fieldInfo.FieldName)).ToList();
        var tableFieldInfos = typeof(TEntity).GetEntityInfo().FieldInfos;
        switch (SqlAdapter.DbType)
        {
            case DatabaseType.MySql:
            case DatabaseType.MariaDB:
            case DatabaseType.TiDB:
            case DatabaseType.OceanBase:
            case DatabaseType.SQLite:
                if (_parameter is IEnumerable<TEntity> entities)// BulkInsertOrUpdate
                {
                    #region 解析批量新增的参数
                    var paramDic = new Dictionary<string, object>();
                    var index = 0;
                    var insertValueParams = new List<string>();
                    var formatParameterNames = new List<string>();
                    foreach (var entity in entities)
                    {
                        index++;
                        formatParameterNames.Clear();
                        foreach (var field in fields)
                        {
                            var findFieldInfo = tableFieldInfos.Find(c => c.FieldName == field.FieldName);
                            if (findFieldInfo == null)
                            {
                                throw new InvalidOperationException($"Table [{field.TableName}] field [{field.FieldName}] not found in [{typeof(TEntity).FullName}].");
                            }

                            if (!SqlParameterized)
                            {
                                var property = findFieldInfo.Property;
                                if (property != null)
                                {
                                    var value = property.GetValue(entity);
                                    var convertResult = SqlBuilderUtil.ConvertToSqlString(SqlAdapter.DbType, value, out var convertible);
                                    if (convertible)
                                    {
                                        formatParameterNames.Add(convertResult);
                                        continue;
                                    }
                                }
                            }

                            var parameterName = ConditionBuilder.UniqueParameter($"{findFieldInfo.Property.Name}_{index}", paramDic);
                            formatParameterNames.Add(SqlAdapter.FormatSqlParameter(parameterName));
                            paramDic.Add(parameterName, findFieldInfo.Property.GetValue(entity, null));
                        }
                        insertValueParams.Add($"({string.Join(", ", formatParameterNames)})");
                    }

                    var bulkInsertValuesString = string.Join($", {(SqlIndented ? Environment.NewLine : string.Empty)}", insertValueParams);
                    SetParameter(paramDic);
                    #endregion

                    sb.Append(string.Format(SqlIndented ? SqlIndentedTemplate : SqlTemplate, SqlAdapter.FormatTableName(), string.Join(", ", formatFields), bulkInsertValuesString));
                }
                else
                {
                    var formatParameters = fields.Select(fieldInfo =>
                    {
                        var findFieldInfo = tableFieldInfos.Find(c => c.FieldName == fieldInfo.FieldName);

                        if (!SqlParameterized)
                        {
                            var property = findFieldInfo?.Property;
                            if (property != null)
                            {
                                var value = property.GetValue(_parameter);
                                var convertResult = SqlBuilderUtil.ConvertToSqlString(SqlAdapter.DbType, value, out var convertible);
                                if (convertible)
                                {
                                    return convertResult;
                                }
                            }
                        }

                        var parameterName = findFieldInfo?.Property.Name ?? fieldInfo.FieldName;
                        return SqlAdapter.FormatSqlParameter(parameterName);
                    }).ToList();
                    sb.Append(string.Format(SqlIndented ? SqlIndentedTemplate : SqlTemplate, SqlAdapter.FormatTableName(), string.Join(", ", formatFields), $"({string.Join(", ", formatParameters)})"));
                }
                break;
            case DatabaseType.QuestDB:
            case DatabaseType.DuckDB:
            case DatabaseType.Xugu:
            default:
                throw new NotSupportedException($"[{SqlAdapter.DbType}]The database does not support the 'REPLACE INTO' SQL syntax.");
        }

        var sql = new DefaultSqlCommand(SqlAdapter.DbType)
        {
            Sql = sb.ToString(),
            Parameter = _parameter
        };
        return sql;
    }
}